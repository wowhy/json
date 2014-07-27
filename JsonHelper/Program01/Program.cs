using JsonHelper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Program01
{
    public class DemoItem
    {
        public string Name { get; set; }
        public double Price { get; set; }
        public bool IsSale { get; set; }
    }

    public class Demo
    {
        public int Id { get; set; }
        public List<DemoItem> Items { get; set; }
        public DateTime Date { get; set; }
    }

    public struct Point
    {
        public int X;
        public int Y;
    }

    class Program
    {
        static void SetPointX(ref object point, object x)
        {
            var tmp = (Point)point;
            tmp.X = (int)x;
            point = tmp;
        }

        static void SetDemo(ref object demo, object id)
        {
            var tmp = (Demo)demo;
            tmp.Id = (int)id;
        }

        static void test()
        {
            var p = (object)new Point() { X = 1, Y = 2 };
            //SetPointX(ref p, 10.0);
            var setter = typeof(Point).GetSetter(typeof(Point).GetField("X"));
            setter(ref p, 10);

            Console.WriteLine("p.X = {0}, p.Y = {1}", ((Point)p).X, ((Point)p).Y);
        }

        private static void test2()
        {
            //            var json =
            //@"
            //{
            //  ""Id"": 10010,
            //  ""Items"":[
            //    { ""Name"":""Apple\u8FD9"", ""Price"":12.3, ""IsSale"": false },
            //    { ""Name"":""Grape"", ""Price"":3.21, ""IsSale"": true }
            //  ],
            //  ""Date"":""2014/03/18""
            //}
            //";
            //            var demo = JSON.ToObject<Demo>(json);

            var type = typeof(Demo);
            var prop = type.GetProperty("Id");
            var value = 10010;
            var count = 10000000;

            var action1 = new Action(() =>
            {
                for (int i = 0; i < count; i++)
                {
                    var demo = Activator.CreateInstance(type);
                    prop.SetValue(demo, value);
                }
            });

            var action2 = new Action(() => 
            {
                for (int i = 0; i < count; i++)
                {
                    var demo = new Demo();
                    demo.Id = value;
                }
            });

            var action3 = new Action(() =>
            {
                var setter = type.GetSetter(prop);
                var creator = type.GetCreator();

                for (int i = 0; i < 10000000; i++)
                {
                    var demo = creator();
                    setter(ref demo, 10010);
                }
            }); 
            
            Takes(action1);
            Takes(action2);
            Takes(action3);
        }


        static void Main(string[] args)
        {
            //test(); return;

            //test2(); return;

            //var data1 = "   [1 , 2 , 3 , 4    , 5,7.12,3.14  ]";
            //var data2 = @"[""Hello, World!\\"", "" \"" ""]";
            //var data3 = @"{  ""id"" : 1, ""name"": ""姓名""}";

            var data =
@"
{
  ""Id"": 10010,
  ""Items"":[
    { ""Name"":""Apple\u8FD9"", ""Price"":12.3, ""IsSale"": false },
    { ""Name"":""Grape"", ""Price"":3.21, ""IsSale"": true }
  ],
  ""Date"":""2014/03/18""
}
";

            int counts = 100000;

            //var parser = new JsonParser(data);
            //var result = parser.Parse();
            //Console.WriteLine(result);

            //Takes(() =>
            //{
            //    for (int i = 0; i < counts; i++)
            //    {
            //        var demo = Newtonsoft.Json.JsonConvert.DeserializeObject<Demo>(data);
            //    }
            //});

            Takes(() =>
            {
                for (int i = 0; i < counts; i++)
                {
                    var demo = JSON.ToObject<Demo>(data);
                }
            });

            // Console.ReadKey();
	}

        static void Takes(Action action)
        {
            var watch = new Stopwatch();
            watch.Start();

            action();

            Console.WriteLine("takes {0} ms.", watch.ElapsedMilliseconds);
        }
    }
}
