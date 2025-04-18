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
	Task SetVolume(float level);

	/// <summary>
	/// Gets music player volume
	/// </summary>
	/// <returns>0 to max</returns>
	Task<float> GetVolume();

	/// <summary>
	/// Gets music player's configured max volume.
	/// This may e.g. be "2.0" if MusicCat is configured to a maximum volume to 2.0.
	/// This is useful so "1.0"/100% can be the default, but silent songs can be cranked up beyond that.
	/// </summary>
	/// <returns>max</returns>
	float MaxVolume { get; }

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