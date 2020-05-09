package main

import (
	"crypto/sha256"
	"encoding/base64"
	"encoding/binary"
	"encoding/json"
	"log"
	"math/rand"
	"net/http"
	"sync"
	"time"
)

type NatType string

const (
	Unspecified          NatType = "Unspecified"
	OpenInternet         NatType = "OpenInternet"
	FullCone             NatType = "FullCone"
	Restricted           NatType = "Restricted"
	PortRestricted       NatType = "PortRestricted"
	Symmetric            NatType = "Symmetric"
	SymmetricUDPFirewall NatType = "SymmetricUDPFirewall"
)

var TimeoutMessage = []byte("matching timeout")

type PeerInfo struct {
	ID            int32  `json:"id"`
	EndPoint      string `json:"endpoint"`
	LocalEndPoint string `json:"local_endpoint"`
	Randam        string `json:"randam"`
}

type Request struct {
	NatType       string `json:"nattype"`
	EndPoint      string `json:"endpoint"`
	LocalEndPoint string `json:"local_endpoint"`
}

type Response struct {
	ID    int32      `json:"id"`
	Peers []PeerInfo `json:"peers"`
}

var match = newMatchMaker()

func main() {
	rand.Seed(time.Now().UnixNano())
	http.HandleFunc("/", handler)
	if err := http.ListenAndServe(":8080", nil); err != nil {
		log.Print(err)
	}
}

func handler(w http.ResponseWriter, r *http.Request) {
	d := json.NewDecoder(r.Body)
	var request Request
	if err := d.Decode(&request); err != nil {
		log.Print(err)
		w.WriteHeader(http.StatusBadRequest)
		w.Write([]byte(err.Error()))
		return
	}
	log.Print(request)
	entry := newMatchEntry(w, &request)
	entry.wg.Add(1)
	match.Join(entry)
	entry.wg.Wait()
}

type matchMaker struct {
	mu             sync.Mutex
	entries        []*matchEntry
	number         int
	hashBaseBuffer []byte
}

type matchEntry struct {
	http.ResponseWriter
	endPoint      string
	localEndPoint string
	wg            sync.WaitGroup
	id            int32
	salt          int32
}

func newMatchMaker() *matchMaker {
	return &matchMaker{
		mu:             sync.Mutex{},
		entries:        make([]*matchEntry, 4),
		number:         0,
		hashBaseBuffer: make([]byte, 16),
	}
}

func newMatchEntry(w http.ResponseWriter, request *Request) *matchEntry {
	return &matchEntry{w, request.EndPoint, request.LocalEndPoint, sync.WaitGroup{}, rand.Int31(), rand.Int31()}
}

func (m *matchMaker) Join(entry *matchEntry) {
	m.mu.Lock()
	defer m.mu.Unlock()
	m.entries[m.number] = entry
	m.number++
	if cap(m.entries) == m.number {
		m.Match()
		return
	}
	timer := time.NewTimer(time.Second * 3)
	go func() {
		<-timer.C
		m.Timeout(entry)
	}()
}

func (m *matchMaker) Reset() {
	m.number = 0
	for i := range m.entries {
		m.entries[i] = nil
	}
}

func (m *matchMaker) Match() {
	defer m.Reset()
	log.Print("Match")
	for _, entry := range m.entries {
		if entry == nil {
			break
		}
		d := json.NewEncoder(entry.ResponseWriter)
		res := m.GetResponse(entry)
		d.Encode(res)
		entry.wg.Done()
		log.Print(entry)
		log.Print(res)
	}
}

func (m *matchMaker) GetResponse(target *matchEntry) *Response {
	peers := make([]PeerInfo, m.number)
	for i, entry := range m.entries {
		var left *matchEntry
		var right *matchEntry
		if entry.salt > target.salt {
			left = entry
			right = target
		} else {
			left = target
			right = entry
		}
		binary.LittleEndian.PutUint32(m.hashBaseBuffer[0:4], uint32(left.salt))
		binary.LittleEndian.PutUint32(m.hashBaseBuffer[4:8], uint32(left.id))
		binary.LittleEndian.PutUint32(m.hashBaseBuffer[8:12], uint32(right.salt))
		binary.LittleEndian.PutUint32(m.hashBaseBuffer[12:16], uint32(right.id))
		hash := sha256.Sum256(m.hashBaseBuffer)

		peers[i] = PeerInfo{
			ID:            entry.id,
			EndPoint:      entry.endPoint,
			LocalEndPoint: entry.localEndPoint,
			Randam:        base64.StdEncoding.EncodeToString(hash[:22]),
		}
	}
	return &Response{
		ID:    target.id,
		Peers: peers,
	}
}

func (m *matchMaker) Timeout(entry *matchEntry) {
	m.mu.Lock()
	defer m.mu.Unlock()
	if len(m.entries) == 0 {
		return
	}
	if m.number == 1 && m.entries[0] == entry {
		log.Print("Timeout", entry)
		defer m.Reset()
		entry.ResponseWriter.WriteHeader(http.StatusRequestTimeout)
		entry.ResponseWriter.Write(TimeoutMessage)
		entry.wg.Done()
		return
	}
	for _, item := range m.entries {
		if item == entry {
			m.Match()
			return
		}
	}
}
