using System.IO;
using System.Text;

namespace SRNet
{
	public static class Json
	{

		public static string To(object obj)
		{
#if UNITY_5_OR_NEWER
			return UnityEngine.JsonUtility.ToJson(obj);
#else
			using (var ms = new MemoryStream())
			{
				var ser = new System.Runtime.Serialization.Json.DataContractJsonSerializer(obj.GetType());
				ser.WriteObject(ms, obj);
				return Encoding.UTF8.GetString(ms.ToArray());
			}
#endif
		}

		public static T From<T>(string json)
		{
#if UNITY_5_OR_NEWER
			return UnityEngine.JsonUtility.FromJson<T>(json);
#else
			using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
			{
				var ser = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(T));
				return (T)ser.ReadObject(ms);
			}
#endif
		}

	}

}