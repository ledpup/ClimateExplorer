using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

namespace ConsoleApp1
{
    public class CountryFileProcessor
    {
        public static async Task<Dictionary<string, Country>> Transform()
        {
            var countryFile = (await File.ReadAllLinesAsync(@"SiteMetaData\country-list.txt")).Skip(2);

            var countries = new Dictionary<string, Country>();

            foreach (var country in countryFile)
            {
                var fields = country.Split("          ");

                if (countries.ContainsKey(fields[0].Trim()))
                {
                    continue;
                }

                countries.Add(fields[0].Trim(),
                    new Country
                    { 
                        Code = fields[0].Trim(),
                        Name = fields[1].Trim(),
                    });
            }

            return countries;
        }
    }

    public class Country
    {
        public string Name { get; set; }
        public string Code { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
