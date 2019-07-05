using System;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.Serialization;

namespace MusicCat.Metadata
{
	public class Metadata
	{
		public List<Song> songs;
		public string id { get; set; }
		public string title { get; set; }
		public string series { get; set; } = null;
		public string year { get; set; }

		[YamlIgnore]
		public string[] platform { get; set; }

		[YamlMember(Alias = "platform", ApplyNamingConventions = false)]
		public dynamic platformFake
		{
			get => platform;
			set
			{
				if (value == null)
				{
					platform = null;
				}
				else
				{
					Type type = value.GetType();
					if (type.IsArray || typeof(IEnumerable<object>).IsAssignableFrom(type))
					{
						List<string> platforms = new List<string>();
						foreach (object obj in (IEnumerable<object>)value)
						{
							platforms.Add(obj.ToString());
						}

						platform = platforms.ToArray();
					}
					else
					{
						platform = new string[] { value.ToString() };
					}
				}
			}
		}

		public bool is_fanwork { get; set; } = false;
	}
}
