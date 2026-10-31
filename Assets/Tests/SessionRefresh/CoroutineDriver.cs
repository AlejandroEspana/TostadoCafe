using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;

namespace ToastedVr.SessionRefresh.Tests
{
    // Hace en edit mode lo que StartCoroutine hace en juego: avanza varias corrutinas, con las anidadas, un frame a la vez.
    public sealed class CoroutineDriver
    {
        // Por tiempo y no por frames: en batchmode los frames son muy rápidos y un puerto cerrado tarda segundos en responder.
        private const double MaxSeconds = 30;

        private readonly List<Stack<IEnumerator>> routines = new List<Stack<IEnumerator>>();

        public void Start(IEnumerator routine)
        {
            var stack = new Stack<IEnumerator>();
            stack.Push(routine);
            routines.Add(stack);
        }

        public IEnumerator RunUntilDone()
        {
            var clock = System.Diagnostics.Stopwatch.StartNew();
            while (Step())
            {
                Assert.Less(clock.Elapsed.TotalSeconds, MaxSeconds, "Las corrutinas no terminaron");
                yield return null;
            }
        }

        private bool Step()
        {
            foreach (var stack in routines.ToArray())
            {
                while (stack.Count > 0)
                {
                    var current = stack.Peek();
                    if (!current.MoveNext())
                    {
                        stack.Pop();
                        continue;
                    }

                    if (current.Current is IEnumerator nested)
                    {
                        stack.Push(nested);
                        continue;
                    }

                    break;
                }
            }

            routines.RemoveAll(stack => stack.Count == 0);
            return routines.Count > 0;
        }
    }
}
