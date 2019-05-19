using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using YamlDotNet.Serialization;

namespace MusicCat.Metadata
{
	public class Song
	{
		public string id { get; set; }
		public string path { get; set; }
		public string title { get; set; }
		[JsonProperty("types", ItemConverterType = typeof(StringEnumConverter))]
		[YamlIgnore]
		public SongType[] types { get; set; }
		[JsonIgnore]
		[YamlMember(Alias = "type", ApplyNamingConventions = false)]
		public dynamic typeFake
		{
			get => types;
			set
			{
				if (value == null)
				{
					types = null;
				}
				else
				{
					Type type = value.GetType();
					if (type.IsArray && type.GetElementType().IsEnum)
					{
						List<SongType> songTypes = new List<SongType>();
						foreach (object obj in (object[]) value)
						{
							songTypes.Add((SongType) Enum.Parse(typeof(SongType), obj.ToString()));
						}

						types = songTypes.ToArray();
					}
					else
					{
						types = new[] {(SongType) Enum.Parse(typeof(SongType), value.ToString())};
					}
				}
			}
		}

		[YamlIgnore]
		public float[] ends { get; set; } = null;
		[JsonIgnore]
		[YamlMember(Alias = "ends", ApplyNamingConventions = false)]
		public dynamic endsFake
		{
			get => ends;
			set
			{
				Type type = value.GetType();
				if (type == typeof(float[]))
				{
					ends = value;
				}
				else if (type == typeof(int[]))
				{
					ends = ((int[]) value).Select(x => float.Parse(x.ToString())).ToArray();
				}
				else if (type == typeof(float))
				{
					ends = new float[] { value };
				}
				else if (type == typeof(int))
				{
					ends = new float[] { float.Parse(value.ToString()) };
				}
				else
				{
					string[] split = value.ToString().Split(':');
					int resultInt = int.Parse(split[0]) * 60 + int.Parse(split[1]);
					ends = new[] { float.Parse(resultInt.ToString()) };
				}
			}
		}
		public string[] tags { get; set; } = null;
		[YamlIgnore]
		public Game game { get; set; }
	}

	public enum SongType
	{
		result,
		betting,
		battle,
		warning,
		@break
	}
}
