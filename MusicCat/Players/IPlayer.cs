using System.Threading.Tasks;

namespace MusicCat.Players
{
    public interface IPlayer
    {
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
        /// Moves playhead
        /// </summary>
        /// <param name="percent">0 to 1</param>
        Task SetPosition(float percent);

        /// <summary>
        /// Gets playhead position
        /// </summary>
        /// <returns>0 to 1</returns>
        Task<float> GetPosition();
    }
}
