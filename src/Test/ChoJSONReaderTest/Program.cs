﻿using ChoETL;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ChoJSONReaderTest
{
    public enum ChoHL7Version
    {
        v2_1,
        v2_2,
        v2_3
    }

    public class MenuItem
    {
        public string Value { get; set; }
        public string OnClick { get; set; }
    }

    [Serializable]
    public class MyObjectType
    {
        [ChoJSONRecordField(JSONPath = "$.id")]
        public string Id { get; set; }
        [ChoJSONRecordField(JSONPath = "$.value")]
        [ChoDefaultValue("FileMenu")]
        public string Value1 { get; set; }

        [XmlElement]
        [ChoJSONRecordField(JSONPath = "$.popup.menuitem")]
        public MenuItem[] MenuItems { get; set; }
    }

    public class Message
    {
        public string Base
        {
            get;
            set;
        }
        public Dictionary<string, string> Rates
        {
            get;
            set;
        }
    }
    public class Product
    {
        public string Name { get; set; }
        public double Price { get; set; }
    }

    class Program
    {
        public class Customer
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
            // need to flatten these lists
            public List<CreditCard> CreditCards { get; set; }
            public List<Address> Addresses { get; set; }
        }

        public class CreditCard
        {
            public string Name { get; set; }
        }

        public class Address
        {
            public string Street { get; set; }
        }

        public static void Test()
        {
            List<Customer> allCustomers = GetAllCustomers();
            var result = allCustomers
   .Select(customer => new[]
   {
      customer.FirstName,
      customer.LastName
   }
   .Concat(customer.CreditCards.Select(cc => cc.Name))
   .Concat(customer.Addresses.Select(address => address.Street)));

            foreach (var c in result)
                Console.WriteLine(ChoUtility.ToStringEx(c.ToList().ToExpandoObject()));
            return;
            //  Customer has CreditCards list and Addresses list

            // how to flatten Customer, CreditCards list, and Addresses list into one flattened record/list?

            var flatenned = from c in allCustomers
                            select
                                c.FirstName + ", " +
                                c.LastName + ", " +
                                String.Join(", ", c.CreditCards.Select(c2 => c2.Name).ToArray()) + ", " +
                                String.Join(", ", c.Addresses.Select(a => a.Street).ToArray());

            flatenned.ToList().ForEach(Console.WriteLine);
        }

        private static List<Customer> GetAllCustomers()
        {
            return new List<Customer>
                   {
                       new Customer
                           {
                               FirstName = "Joe",
                               LastName = "Blow",
                               CreditCards = new List<CreditCard>
                                                 {
                                                     new CreditCard
                                                         {
                                                             Name = "Visa"
                                                         },
                                                     new CreditCard
                                                         {
                                                             Name = "Master Card"
                                                         }
                                                 },
                               Addresses = new List<Address>
                                               {
                                                   new Address
                                                       {
                                                           Street = "38 Oak Street"
                                                       },
                                                   new Address
                                                       {
                                                           Street = "432 Main Avenue"
                                                       }
                                               }
                           },
                       new Customer
                           {
                               FirstName = "Sally",
                               LastName = "Cupcake",
                               CreditCards = new List<CreditCard>
                                                 {
                                                     new CreditCard
                                                         {
                                                             Name = "Discover"
                                                         },
                                                     new CreditCard
                                                         {
                                                             Name = "Master Card"
                                                         }
                                                 },
                               Addresses = new List<Address>
                                               {
                                                   new Address
                                                       {
                                                           Street = "29 Maple Grove"
                                                       },
                                                   new Address
                                                       {
                                                           Street = "887 Nut Street"
                                                       }
                                               }
                           }
                   };
        }
        private static string EmpJSON = @"    
        [
          {
            ""Id"": 1,
            ""Name"": ""Raj"",
            ""Courses"": [ ""Math"", ""Tamil""],
            ""Dict"": {""key1"":""value1"",""key2"":""value2""}
          },
          {
            ""Id"": 2,
            ""Name"": ""Tom"",
          }
        ]
        ";

        private static string Stores = @"{
  'Stores': [
    'Lambton Quay',
    'Willis Street'
  ],
  'Manufacturers': [
    {
      'Name': 'Acme Co',
      'Products': [
        {
          'Name': 'Anvil',
          'Price': 50
        }
      ]
    },
    {
      'Name': 'Contoso',
      'Products': [
        {
          'Name': 'Elbow Grease',
          'Price': 99.95
        },
        {
          'Name': 'Headlight Fluid',
          'Price': 4
        }
      ]
    }
  ]}            ";

        static void Main(string[] args)
        {
            IgnoreItems();
        }

        static void IgnoreItems()
        {
            using (var jr = new ChoJSONReader("sample6.json")
                .WithField("ProductId", jsonPath: "$.productId")
                .WithField("User", jsonPath: "$.returnPolicies.user")
                )
            {
                foreach (var item in jr)
                    Console.WriteLine(item.ProductId + " " + item.User);
            }
        }
        public static void KVPTest()
        {
            using (var jr = new ChoJSONReader<Dictionary<string, string>>("sample5.json").Configure(c => c.UseJSONSerialization = true))
            {
                foreach (var dict1 in jr.Select(dict => dict.Select(kvp => new { kvp.Key, kvp.Value })).SelectMany(x => x))
                {
                    Console.WriteLine(dict1.Key);
                }
            }
        }

        static void Sample4()
        {
            using (var jr = new ChoJSONReader("sample4.json").Configure(c => c.UseJSONSerialization = true))
            {
                using (var xw = new ChoCSVWriter("sample4.csv").WithFirstLineHeader())
                {
                    foreach (JObject jItem in jr)
                    {
                        dynamic item = jItem;
                        var identifiers = ChoEnumerable.AsEnumerable<JObject>(jItem).Select(e => ((IList<JToken>)((dynamic)e).identifiers).Select(i =>
                           new
                           {
                               identityText = i["identityText"].ToString(),
                               identityTypeCode = i["identityTypeCode"].ToString()
                           })).SelectMany(x => x);

                        var members = ChoEnumerable.AsEnumerable<JObject>(jItem).Select(e => ((IList<JToken>)((dynamic)e).members).Select(m => ((IList<JToken>)((dynamic)m).identifiers).Select(i =>
                           new
                           {
                               dob = m["dob"].ToString(),
                               firstName = m["firstName"].ToString(),
                               gender = m["gender"].ToString(),
                               identityText = i["identityText"].ToString(),
                               identityTypeCode = i["identityTypeCode"].ToString(),
                               lastname = m["lastName"].ToString(),
                               memberId = m["memberId"].ToString(),
                               optOutIndicator = m["optOutIndicator"].ToString(),
                               relationship = m["relationship"].ToString()

                           }))).SelectMany(x => x).SelectMany(y => y);

                        var comb = members.ZipEx(identifiers, (m, i) =>
                        {
                            if (i == null)
                                return new
                                {
                                    item.ccId,
                                    item.hId,
                                    identifiers_identityText = String.Empty,
                                    identifiers_identityTypeCode = String.Empty,
                                    members_dob = m.dob,
                                    members_firstName = m.firstName,
                                    members_gender = m.gender,
                                    members_identifiers_identityText = m.identityText,
                                    members_identityTypeCode = m.identityTypeCode,
                                    members_lastname = m.lastname,
                                    members_memberid = m.memberId,
                                    member_optOutIndicator = m.optOutIndicator,
                                    member_relationship = m.relationship,
                                    SubscriberFirstName = item.subscriberFirstame,
                                    SubscriberLastName = item.subscriberLastName,

                                };
                            else
                                return new
                                {
                                    item.ccId,
                                    item.hId,
                                    identifiers_identityText = i.identityText,
                                    identifiers_identityTypeCode = i.identityTypeCode,
                                    members_dob = m.dob,
                                    members_firstName = m.firstName,
                                    members_gender = m.gender,
                                    members_identifiers_identityText = m.identityText,
                                    members_identityTypeCode = m.identityTypeCode,
                                    members_lastname = m.lastname,
                                    members_memberid = m.memberId,
                                    member_optOutIndicator = m.optOutIndicator,
                                    member_relationship = m.relationship,
                                    SubscriberFirstName = item.subscriberFirstame,
                                    SubscriberLastName = item.subscriberLastName,
                                };

                        });
                        xw.Write(comb);
                    }
                }
            }

            //foreach (var e in jr.Select(i => new[] { i.ccId.ToString(), i.hId.ToString() }
            //.Concat(((IList<JToken>)i.identifiers).Select(jt => jt["identityText"].ToString()))
            //.Concat(((IList<JToken>)i.members).Select(jt => jt["dob"].ToString()))
            //)
            //)
            //    xw.Write(e.ToList().ToExpandoObject());

            //foreach (var e in jr.Select(i => new { i.ccId, i.hId, identityText = ((IList<JToken>)i.identifiers).Select(x => x["identityText"]) }))
            //{

            //}
        }

        static void Sample3()
        {
            using (var jr = new ChoJSONReader<MyObjectType>("sample3.json").WithJSONPath("$.menu")
                )
            {
                jr.AfterRecordFieldLoad += (o, e) =>
                {
                };
                using (var xw = new ChoXmlWriter<MyObjectType>("sample3.xml").Configure(c => c.UseXmlSerialization = true))
                    xw.Write(jr);
            }
        }

        static void Sample2()
        {
            //using (var csv = new ChoCSVWriter("sample2.csv") { TraceSwitch = ChoETLFramework.TraceSwitchOff }.WithFirstLineHeader())
            //{
            //    csv.Write(new ChoJSONReader("sample2.json") { TraceSwitch = ChoETLFramework.TraceSwitchOff }
            //    .WithField("Base")
            //    .WithField("Rates", fieldType: typeof(Dictionary<string, object>))
            //    .Select(m => ((Dictionary<string, object>)m.Rates).Select(r => new { Base = m.Base, Key = r.Key, Value = r.Value })).SelectMany(m => m)
            //    );
            //}
        }

        static void Sample1()
        {
            using (var csv = new ChoCSVWriter("sample1.csv") { TraceSwitch = ChoETLFramework.TraceSwitchOff }.WithFirstLineHeader())
            {
                csv.Write(new ChoJSONReader("sample1.json") { TraceSwitch = ChoETLFramework.TraceSwitchOff }.Select(e => Flatten(e)));
            }
        }
        private static object[] Flatten(dynamic e)
        {
            List<object> list = new List<object>();
            list.Add(new { F1 = e.F1, F2 = e.F2, E1 = String.Empty, E2 = String.Empty, D1 = String.Empty, D2 = String.Empty });
            foreach (var se in e.F3)
            {
                if (se["E3"] != null)
                {
                    foreach (var de in se.E3)
                        list.Add(new { F1 = e.F1, F2 = e.F2, E1 = se.E1, E2 = se.E2, D1 = de.D1, D2 = de.D2 });
                }
                else
                    list.Add(new { F1 = e.F1, F2 = e.F2, E1 = se.E1, E2 = se.E2, D1 = String.Empty, D2 = String.Empty });
            }
            return list.ToArray();
        }
        static void JsonToXml()
        {
            using (var csv = new ChoXmlWriter("companies.xml") { TraceSwitch = ChoETLFramework.TraceSwitchOff }.WithXPath("companies/company"))
            {
                csv.Write(new ChoJSONReader<Company>("companies.json") { TraceSwitch = ChoETLFramework.TraceSwitchOff }.NotifyAfter(10000).Take(10).
                    SelectMany(c => c.Products.Touch().
                    Select(p => new { c.name, c.Permalink, prod_name = p.name, prod_permalink = p.Permalink })));
            }
        }

        static void JsonToCSV()
        {
            using (var csv = new ChoCSVWriter("companies.csv") { TraceSwitch = ChoETLFramework.TraceSwitchOff }.WithFirstLineHeader())
            {
                csv.Write(new ChoJSONReader<Company>("companies.json") { TraceSwitch = ChoETLFramework.TraceSwitchOff }.NotifyAfter(10000).Take(10).
                    SelectMany(c => c.Products.Touch().
                    Select(p => new { c.name, c.Permalink, prod_name = p.name, prod_permalink = p.Permalink })));
            }
        }

        static void LoadTest()
        {
            using (var p = new ChoJSONReader<Company>("companies.json") { TraceSwitch = ChoETLFramework.TraceSwitchOff }.NotifyAfter(10000))
            {
                p.Configuration.ColumnCountStrict = true;
                foreach (var e in p)
                    Console.WriteLine("overview: " + e.name);
            }

            //Console.WriteLine("Id: " + e.name);
        }

        public class Product
        {
            [ChoJSONRecordField]
            public string name { get; set; }
            [ChoJSONRecordField]
            public string Permalink { get; set; }
        }

        public class Company
        {
            [ChoJSONRecordField]
            public string name { get; set; }
            [ChoJSONRecordField]
            public string Permalink { get; set; }
            [ChoJSONRecordField(JSONPath = "$.products")]
            public Product[] Products { get; set; }
        }
        static void QuickLoad()
        {
            foreach (dynamic e in new ChoJSONReader("Emp.json"))
                Console.WriteLine("Id: " + e.Id + " Name: " + e.Name);
        }

        static void POCOTest()
        {
            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoJSONReader<EmployeeRec>(reader))
            {
                writer.WriteLine(EmpJSON);

                writer.Flush();
                stream.Position = 0;

                object rec;
                while ((rec = parser.Read()) != null)
                {
                    Console.WriteLine(rec.ToStringEx());
                }
            }
        }
        static void StorePOCOTest()
        {
            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoJSONReader<StoreRec>(reader).WithJSONPath("$.Manufacturers"))
            {
                writer.WriteLine(Stores);

                writer.Flush();
                stream.Position = 0;

                object rec;
                while ((rec = parser.Read()) != null)
                {
                    Console.WriteLine(rec.ToStringEx());
                }
            }
        }
        static void StorePOCONodeLoadTest()
        {
            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var jparser = new JsonTextReader(reader))
            {
                writer.WriteLine(Stores);

                writer.Flush();
                stream.Position = 0;

                var config = new ChoJSONRecordConfiguration() { UseJSONSerialization = true };
                object rec;
                using (var parser = new ChoJSONReader<StoreRec>(JObject.Load(jparser).SelectTokens("$.Manufacturers"), config))
                {
                    while ((rec = parser.Read()) != null)
                    {
                        Console.WriteLine(rec.ToStringEx());
                    }
                }
            }
        }
        static void QuickLoadTest()
        {
            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoJSONReader(reader).WithJSONPath("$.Manufacturers").WithField("Name", fieldType: typeof(string)).WithField("Products", fieldType: typeof(Product[])))
            {
                writer.WriteLine(Stores);

                writer.Flush();
                stream.Position = 0;

                object rec;
                while ((rec = parser.Read()) != null)
                {
                    Console.WriteLine(rec.ToStringEx());
                }
            }
        }
        static void QuickLoadSerializationTest()
        {
            var config = new ChoJSONRecordConfiguration() { UseJSONSerialization = false };

            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoJSONReader(reader, config).WithJSONPath("$.Manufacturers"))
            {
                writer.WriteLine(Stores);

                writer.Flush();
                stream.Position = 0;

                object rec;
                while ((rec = parser.Read()) != null)
                {
                    Console.WriteLine(rec.ToStringEx());
                }
            }
        }
        public class EmployeeRec
        {
            public int Id
            {
                get;
                set;
            }
            public string Name
            {
                get;
                set;
            }
            public string[] Courses
            {
                get;
                set;
            }
            public Dictionary<string, string> Dict
            {
                get;
                set;
            }
            public override string ToString()
            {
                return "{0}. {1}. Course Count: {2}. Dict Count: {3}".FormatString(Id, Name, Courses == null ? 0 : Courses.Length, Dict == null ? 0 : Dict.Count);
            }
        }
        public class ProductRec
        {
            public string Name
            {
                get;
                set;
            }
            public string Price
            {
                get;
                set;
            }
        }
        public class StoreRec
        {
            public string Name
            {
                get;
                set;
            }
            public ProductRec[] Products
            {
                get;
                set;
            }
            public override string ToString()
            {
                return "{0}. {1}.".FormatString(Name, Products == null ? 0 : Products.Length);
            }
        }
    }
}
