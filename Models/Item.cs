using System;
using System.Collections.Generic;
using System.Text;

namespace TestMocksGenerator.Models
{
    public class Item
    {
        public Data Data { get; set; }

        public List<Data> DataArray { get; set; }

        public string Name { get; set; }

        public string Value { get; set; }
    }
}
