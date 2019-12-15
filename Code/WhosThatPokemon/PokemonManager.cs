using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver;

namespace WhosThatPokemon
{
    /// <summary>
    /// Manages the pokemon used within the game.
    /// </summary>
    public class PokemonManager
    {
        /// <summary>
        /// Master dictionary of Pokemon where the Pokemon name in all
        /// lowercase is the key and the Pokemon object is the value.
        /// </summary>
        private Dictionary<string, Pokemon> pokemon;

        /// <summary>
        /// Random number generator.
        /// </summary>
        private Random randomGenerator;

        /// <summary>
        /// Creates an instance of the Pokemon Manager class.
        /// </summary>
        public PokemonManager()
        {
            pokemon = new Dictionary<string, Pokemon>();
            randomGenerator = new Random();
        }

        /// <summary>
        /// Determies if two Pokemon belong to same family.
        /// </summary>
        /// <param name="x">First Pokemon name</param>
        /// <param name="y">Second Pokemon name</param>
        /// <returns>True if same family</returns>
        public bool AreFamily(string x, string y)
        {
            if (x == null || y == null)
            {
                Console.WriteLine("AreFamily passed null values");
                return false;
            }

            if(x == y)
            {
                return true;
            }

            // Traverse all previous family members.
            Pokemon temp = GetPokemon(x);
            while (temp != null)
            {
                temp = GetPokemon(temp.PrevEvo);
                if (temp != null && temp.Name == y)
                {
                    return true;
                }
            }

            // Traverse all next family members.
            temp = GetPokemon(x);
            while (temp != null)
            {
                temp = GetPokemon(temp.NextEvo);
                if (temp != null && temp.Name == y)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Retrieves a Pokemon from the dictionary via a name.
        /// </summary>
        /// <param name="name">Pokemon name</param>
        /// <returns>Pokemon if found, null if not found</returns>
        public Pokemon GetPokemon(string name)
        {
            name = name?.ToLower();

            if (name == null || !pokemon.ContainsKey(name))
            {
                return null;
            }

            return pokemon[name];
        }

        /// <summary>
        /// Returns a subset of the pokemon names based on the given predicate.
        /// </summary>
        /// <param name="predicate">Predicate function</param>
        /// <returns>An IEnumerable collection of pokemon names</returns>
        public IEnumerable<Pokemon> GetSubset(Func<Pokemon, bool> predicate)
        {
            return pokemon.Values.Where(predicate);
        }

        /// <summary>
        /// Retrieve Pokemon information from MongoDB collection.
        /// </summary>
        public bool LoadPokemon()
        {
            // Clear any Pokemon currently in dictionary
            pokemon.Clear();

            MongoClient client = new MongoClient();
            var db = client.GetDatabase("WhosThatPokemon");
            var collection = db.GetCollection<Pokemon>("Pokemon");

            // Attempt to connect to database
            if (!db.RunCommandAsync((Command<MongoDB.Bson.BsonDocument>)"{ping:1}").Wait(1000))
            {
                return false;
            }

            List<Pokemon> list = collection.Find(_ => true).ToList();
            if (list.Count == 0)
            {
                Console.WriteLine("No Pokemon found in database");
                return false;
            }
            foreach (Pokemon poke in list)
            {
                pokemon[poke.Name.ToLower()] = poke;
            }

            return true;
        }

        /// <summary>
        /// Returns the number of similar abilities between Pokemon.
        /// </summary>
        /// <param name="a">Pokemon to compare</param>
        /// <param name="b">Pokemon to compare</param>
        /// <returns>Number of similar abilities</returns>
        public int SameAbilityCount(Pokemon a, Pokemon b)
        {
            if(a == null || b == null)
            {
                return 0;
            }

            int count = 0;
            string[] firstabilityArray = a.Abilities ?? new string[0];
            string[] secondAbilityArray = b.Abilities ?? new string[0];

            for (int i = 0; i < firstabilityArray.Length; i++)
            {
                if (secondAbilityArray.Contains(firstabilityArray[i]))
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Returns the number of similar types between Pokemon.
        /// </summary>
        /// <param name="a">Pokemon to compare</param>
        /// <param name="b">Pokemon to compare</param>
        /// <returns>Number of similar types</returns>
        public int SameTypeCount(Pokemon a, Pokemon b)
        {
            if (a == null || b == null)
            {
                return 0;
            }

            int count = 0;
            string[] firstTypeArray = a.Types ?? new string[0];
            string[] secondTypeArray = b.Types ?? new string[0];

            for (int i = 0; i < firstTypeArray.Length; i++)
            {
                if (secondTypeArray.Contains(firstTypeArray[i]))
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Selects a random pokemon from within the Pokemon array
        /// </summary>
        /// <param name="pokemonNames">Array containing selectable pokemon</param>
        /// <returns>Randomly selected Pokemon, null if no Pokemon selected</returns>
        public Pokemon SelectRandomPokemon(Pokemon[] toChoose)
        {
            if (toChoose == null || toChoose.Length == 0)
            {
                Console.WriteLine("No pokemon to chose from");
                return null;
            }

            // Select a Pokemon within the chosen list
            Pokemon randomPokemon;
            do
            {
                randomPokemon = toChoose[randomGenerator.Next(0, toChoose.Length)];
            }
            while (randomPokemon == null);

            return randomPokemon;
        }
    }
}