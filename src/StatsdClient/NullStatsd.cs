using System;
using System.Collections.Generic;

namespace StatsdClient
{
    public class NullStatsd : IStatsd
    {
        public NullStatsd()
        {
            Commands = new List<string>();
        }

        public List<string> Commands { get; private set; }

        public void Send<TCommandType>(string name, int value) where TCommandType : IAllowsInteger
        {
        }

        public void Add<TCommandType>(string name, int value) where TCommandType : IAllowsInteger
        {
        }

        public void AddGauge(string name, double value)
        {
        }

        public void Send<TCommandType>(string name, int value, double sampleRate)
            where TCommandType : IAllowsInteger, IAllowsSampleRate
        {
        }

        public void Add<TCommandType>(string name, int value, double sampleRate)
            where TCommandType : IAllowsInteger, IAllowsSampleRate
        {
        }

        public void SendSet(string name, string value) 
        {
        }

        public void Send()
        {
        }

        public void Add(Action actionToTime, string statName, double sampleRate = 1)
        {
            actionToTime();
        }

        public void Send(Action actionToTime, string statName, double sampleRate = 1)
        {
            actionToTime();
        }

        public void SendGauge(string name, double value, bool isDeltaValue)
        {
        }
  }
}