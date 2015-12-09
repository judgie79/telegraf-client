using System;
using System.Collections.Generic;

namespace StatsdClient
{
    public interface IStatsd
    {
        List<string> Commands { get; }
        
        void AddGauge(string name, double value);
		void SendGauge(string name, double value, bool isDeltaValue);

	    void SendInteger(IntegralMetric type, string name, int value, double sampleRate);
	    void AddInteger(IntegralMetric type, string name, int value, double sampleRate); 

        void SendSet(string name, string value) ;

        void Send();
    }
}