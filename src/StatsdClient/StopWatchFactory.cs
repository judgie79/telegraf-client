using System.Diagnostics;

namespace StatsdClient
{



	public delegate int StopAndMeasureElapsedMs();
	public delegate StopAndMeasureElapsedMs IStopWatchFactory();

}