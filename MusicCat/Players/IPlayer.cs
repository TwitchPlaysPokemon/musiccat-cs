namespace MusicCat.Players;

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
	/// <param name="level">0 to 1</param>
	/// <returns>the new volume, 0 to max</returns>
	Task<float> SetVolume(float level);

	/// <summary>
	/// Gets music player volume
	/// </summary>
	/// <returns>0 to max</returns>
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
}