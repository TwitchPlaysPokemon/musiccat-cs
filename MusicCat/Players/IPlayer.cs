using System.Collections.Generic;
using System.Threading.Tasks;
using MusicCat.Metadata;

namespace MusicCat.Players
{
    public interface IPlayer
	{
		/// <summary>
		/// Launches the music player.
		/// </summary>
		/// <returns></returns>
		Task Launch();

        /// <summary>
        /// Tells the music player to play the current track
        /// </summary>
        Task Play();

        /// <summary>
        /// Tells the music player to stop playback
        /// </summary>
        Task Stop();

        /// <summary>
        /// Tells the music player to pause playback
        /// </summary>
        Task Pause();

        /// <summary>
        /// Sets music player volume
        /// </summary>
        /// <param name="level">0 to 255</param>
        Task SetVolume(float level);

        /// <summary>
        /// Gets music player volume
        /// </summary>
        /// <returns>0 to 255</returns>
        Task<float> GetVolume();

        /// <summary>
        /// Tells the music player to play a file
        /// </summary>
        /// <param name="filename">Absolute path of file</param>
        Task PlayFile(string filename);

		/// <summary>
		/// Tells the music player to play the song with a specific ID
		/// </summary>
		/// <param name="id">ID of the song to play</param>
	    Task PlayID(string id);

        /// <summary>
        /// Moves playhead
        /// </summary>
        /// <param name="percent">0 to 1</param>
        Task SetPosition(float percent);

        /// <summary>
        /// Gets playhead position
        /// </summary>
        /// <returns>0 to 1</returns>
        Task<float> GetPosition();

		/// <summary>
		/// Gets the number of songs in the library
		/// </summary>
		/// <param name="category">The category of the song, must be parseable into the SongType enum</param>
		/// <returns></returns>
		Task<int> Count(string category = null);

		/// <summary>
		/// Gets a random song across all categories
		/// </summary>
		/// <returns></returns>
		Task<Song> GetRandomSong();

		/// <summary>
		/// Gets a random song that fits a filter
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		Task<Song> GetRandomSongBy(string[] args);

		/// <summary>
		/// Searches through the metadata to provide songs with the closest match
		/// </summary>
		/// <param name="keywords">keywords to search</param>
		/// <param name="requiredTag">any required tag</param>
		/// <param name="cutoff">the cutoff value, with 1.0 being a perfect match</param>
		/// <returns>a list of tuples containing the song and the match ratio</returns>
	    Task<List<(Song song, float match)>> Search(string[] keywords, string requiredTag = null,
		    float cutoff = 0.3f);
    }
}
