using System;
using System.Collections.Generic;
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
					if (type.IsArray || typeof(IEnumerable<object>).IsAssignableFrom(type))
					{
						List<SongType> songTypes = new List<SongType>();
						foreach (object obj in (IEnumerable<object>) value)
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
		public float? ends { get; set; }

		[JsonIgnore]
		[YamlMember(Alias = "ends", ApplyNamingConventions = false)]
		public dynamic endsFake
		{
			get => ends;
			set
			{
				string[] split = value.ToString().Split(':');
				int resultInt = int.Parse(split[0]) * 60 + int.Parse(split[1]);
				ends = float.Parse(resultInt.ToString());
			}
		}
		[YamlIgnore]
		public string[] tags { get; set; } = null;

		[YamlMember(Alias = "tags", ApplyNamingConventions = false)]
		public dynamic platformFake
		{
			get => tags;
			set
			{
				if (value == null)
				{
					tags = null;
				}
				else
				{
					Type type = value.GetType();
					if (type.IsArray || typeof(IEnumerable<object>).IsAssignableFrom(type))
					{
						List<string> tagList = new List<string>();
						foreach (object obj in (IEnumerable<object>)value)
						{
							tagList.Add(obj.ToString());
						}

						tags = tagList.ToArray();
					}
					else
					{
						tags = new string[] { value.ToString() };
					}
				}
			}
		}

		[YamlIgnore]
		public bool canBePlayed = true;

		[YamlIgnore]
		[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
		public DateTime? cooldownExpiry = null;

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
