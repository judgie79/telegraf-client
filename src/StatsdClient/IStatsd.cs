using System;
using System.Collections.Generic;

namespace StatsdClient
{
    public interface IStatsd
    {
        List<string> Commands { get; }
        
        void Send<TCommandType>(string name, int value) where TCommandType : IAllowsInteger;
        void Add<TCommandType>(string name, int value) where TCommandType : IAllowsInteger;

        void SendGauge(string name, double value);
        void AddGauge(string name, double value);
		void SendGauge(string name, double value, bool isDeltaValue);

        void Send<TCommandType>(string name, int value, double sampleRate) where TCommandType : IAllowsInteger, IAllowsSampleRate;
        void Add<TCommandType>(string name, int value, double sampleRate) where TCommandType : IAllowsInteger, IAllowsSampleRate;

        void SendSet(string name, string value) ;

        void Send();

        void Add(Action actionToTime, string statName, double sampleRate=1);
        void Send(Action actionToTime, string statName, double sampleRate=1);
    }
}