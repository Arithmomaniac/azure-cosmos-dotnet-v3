﻿namespace StronglyTypedPatch
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.Azure.Cosmos;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;

    public class PatchTests
    {
        public void TestPathResoluition()
        {
            StronglyTypedPatchOperationFactory<Person> factory = new StronglyTypedPatchOperationFactory<Person>(new DefaultContractResolver());

            string[] actualPaths = new[] {

                factory.Add(person => person.Age, 50),
                factory.Add(person => person.Children, new List<Person> { new Person("Billy", 1, 0) }),
                factory.Add(person => person.Children[-1], new Person("Billy", 25, 0)),
                factory.Add(person => person.Children[0], new Person("Billy", 25, 0)),
                factory.Add(person => person.Children[0].Age, 25),
                factory.Add(person => person.Children[0].Name, "Bill"),
                factory.Add(person => person.Children[0].Salary, 0),
                factory.Add(person => person.Children[0].Children[0], value: new Person("Susie", 1, 0)),
                factory.Add(person => person.Children[0].Children, new List<Person> { new Person("Billy", 1, 0) }),
                factory.Add(person => person.Children[0].Children[-1], new Person("Billy", 25, 0)),
                factory.Add(person => person.Name, "Ted"),
                factory.Add(person => person.Salary, 25000)
            }.Select(x => x.Path)
            .ToArray();

            string[] expectedPaths = new[]
            {
                "/age",
                "/children",
                "/children/-",
                "/children/0",
                "/children/0/age",
                "/children/0/name",
                "/children/0/salary",
                "/children/0/children/0",
                "/children/0/children",
                "/children/0/children/-",
                "/name",
                "/salary",
            };

            actualPaths.Should().BeEquivalentTo(expectedPaths, o => o.WithStrictOrdering());
        }       
    }

    public sealed class Person
    {
        public Person(string name, int age, double salary, IReadOnlyList<Person> children = null)
        {
            this.Name = name;
            this.Age = age;
            this.Salary = salary;
            this.Children = new List<Person>(children ?? Array.Empty<Person>());
        }

        [JsonProperty("name")]
        public string Name { get; }

        [JsonProperty("age")]
        public int Age { get; }

        [JsonProperty("salary")]
        public double Salary { get; }

        [JsonProperty("children")]
        public IReadOnlyList<Person> Children { get; }

        public override bool Equals(object obj)
        {
            return obj is Person person && this.Equals(person);
        }

        public bool Equals(Person other)
        {
            return (this.Name == other.Name) && (this.Age == other.Age);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(this.Name, this.Age);
        }
    }
}
