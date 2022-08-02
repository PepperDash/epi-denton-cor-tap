using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro.CrestronThread;
using PepperDash.Core;

namespace PoeTexasCorTap
{
    public class LightingGatewayQueue : IKeyed
    {
        private readonly Thread _worker;
        private readonly CrestronQueue<Action> _queue = new CrestronQueue<Action>(500);

        public LightingGatewayQueue(string key)
        {
            Key = key;
            _worker = new Thread(ProcessQueue, _queue) {Name = key};
        }

        public string Key { get; private set; }

        private static object ProcessQueue(object o)
        {
            var queue = o as CrestronQueue<Action>;
            if (queue == null)
                throw new NullReferenceException("Queue cannot be null");

            CrestronEnvironment.ProgramStatusEventHandler += type =>
            {
                if (type == eProgramStatusEventType.Stopping)
                {
                    queue.Clear();
                    queue.Enqueue(null);
                }
            };

            while (true)
            {
                var action = queue.Dequeue();
                if (action == null)
                    break;

                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    Debug.Console(2, "Caught an exception in a queue:{0}", ex.Message);
                }
            }

            return null;
        }

        public void Enqueue(Action a)
        {
            _queue.Enqueue(a);
        }
    }
}