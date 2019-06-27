using System;
using System.Collections.Generic;
using System.Text;

namespace MusicCat.Metadata
{
	public class DuplicateSongException : Exception
	{
		public DuplicateSongException(string message)
			: base(message)
		{ }
		
		public DuplicateSongException()
		{ }
	}
}
