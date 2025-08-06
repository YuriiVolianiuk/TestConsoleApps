using System;

namespace TServer
{
    public class QuoteGenerator
    {
        private readonly int _min;
        private readonly int _max;
        private readonly Random _random;
        private long _id;

        public QuoteGenerator(int min, int max)
        {
            _min = min;
            _max = max;
            _random = new Random();
            _id = 0;
        }

        public (long Id, double Value) Next()
        {
            _id++;
            var value = _random.Next(_min, _max + 1);
            return (_id, value);
        }
    }
}
