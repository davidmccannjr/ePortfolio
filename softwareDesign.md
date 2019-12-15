# Software Design / Engineering Enhancement

## Narrative

The "Video Game Collection" project was the project I decided to enhance with a focus on software design and engineering. My artifact is a personal project of mine from a few years ago that displays the video games that I currently own. I originally saved the collection in an XML file that I could load, but my enhancement was to transition the collection over to a MongoDB collection, similar to the database enhancement. This transition to MongoDB would also be important for fulfilling the goal I set for the Algorithms and Data Structures enhancement.
  
This artifact, like the "Who's That Pokemon" artifact, showcases my ability to take an idea and deliver a program that fulfills that vision. I wanted a way to view my gaming collection and various stats related to the collection, and I was able to fully design and develop the project. The improvements I made on the artifact showcase my ability to improve and alter the software design while still maintaining the project functionality. By changing the way the program reads in the initial game information from an XML file to a MongoDB collection, I had to alter the way the Game class was designed to utilize the MongoDB driver in Visual Studio. I also changed how the program saves and updates the collection, as I update the database directly after each action rather than waiting for the user to save their changes as in the original project.
  
In the "Who's That Pokemon" artifact, I transitioned from having information stored in an XML file to using a MongoDB collection. However, that artifact utilized these changes mostly using Python, as I created a RESTful API using the Bottle API and PyMongo. In this enhancement, however, I used Visual Studio, C#, and Windows Forms using the .NET framework. While I was still working with MongoDB, the way I interacted with MongoDB was completely different. PyMongo offered a syntax that was similar to MongoDB, so the transition between the two went pretty smooth. However, the C# Driver used different methods, which I had to familiarize myself with.
  
In addition to the database changes, for this enhancement I went over my code to find ways to improve the program's performance. In my original work, I loaded the game’s cover art when each game was initialized, which caused the program to take a couple of seconds when loading the games. Now, the cover art is loaded when the game’s cover art is displayed on screen, and these are only displayed if the drop down menu tied to the collection the game is stored in is opened, meaning the game cover arts are loaded in sections and only when needed. In the same process, opening and closing the collection  would cause each game within the collection to have their visibility altered, which caused slowdowns. To fix this, I used the SuspendLayout() function when changing the control visibility for all of the GameControl objects, which resulted in remarkable improvements. Overall, I feel I succeeded in meeting my objectives for this enhancement: I improved the program performance, improved my code by finding and fixing defects such as referencing null variables, and changed the way my program read and stored data.


## GameOrganizer Database Class

```c#
using System;
using System.Configuration;
using System.Collections.Generic;
using MongoDB.Driver;
using MongoDB.Bson;

namespace GameOrganizer
{
    /// <summary>
    /// Handles the MongoDB calls for the game collection.
    /// </summary>
    static class MongoDbManager
    {
        /// <summary>
        /// Adds the game to the MongoDB "Games" collection.
        /// </summary>
        /// <param name="toAdd">The game to add to the collection.</param>
        /// <returns>True if add successful</returns>
        public static bool AddGame(Game toAdd)
        {
            if (toAdd == null)
            {
                Console.WriteLine("MongoDbManager AddGame() passed a null value");
                return false;
            }

            MongoClient client = new MongoClient();
            var db = client.GetDatabase(
                ConfigurationManager.AppSettings["Database"]);
            var collection = db.GetCollection<Game>(
                ConfigurationManager.AppSettings["Collection"]);

            try
            {
                collection.InsertOne(toAdd);
            }
            catch(MongoDB.Driver.MongoWriteException)
            {
                Console.WriteLine("Error saving game to collection");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks to see if the server is currently running.
        /// </summary>
        /// <returns>True if connection established</returns>
        public static bool ConnectionEstablished()
        {
            MongoClient client = new MongoClient();
            var db = client.GetDatabase(
                ConfigurationManager.AppSettings["Database"]);
            return db.RunCommandAsync((Command<BsonDocument>)"{ping:1}").Wait(1000);
        }

        /// <summary>
        /// Returns the game information for the game with the given ObjectId.
        /// </summary>
        /// <param name="id">Game ObjectId</param>
        /// <returns>The game with the given ObjectId, null if not found</returns>
        public static Game FindOne(ObjectId id)
        {
            MongoClient client = new MongoClient();
            var db = client.GetDatabase(
                ConfigurationManager.AppSettings["Database"]);
            var collection = db.GetCollection<Game>(
                ConfigurationManager.AppSettings["Collection"]);

            try
            {
                return collection.Find(a => a.Id == id).Single();
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }

        /// <summary>
        /// Finds all the games within the "Games" collection.
        /// </summary>
        /// <returns>List of the games in the collection</returns>
        public static List<Game> FindAll()
        {
            MongoClient client = new MongoClient();
            var db = client.GetDatabase(
                ConfigurationManager.AppSettings["Database"]);
            var collection = db.GetCollection<Game>(
                ConfigurationManager.AppSettings["Collection"]);

            return collection.Find(_ => true).ToList();
        }

        /// <summary>
        /// Removes the given game from the MongoDB "Games" collection.
        /// </summary>
        /// <param name="toRemove">The game to remove from the collection.</param>        
        /// <returns>True if remove successful</returns>
        public static bool RemoveGame(Game toRemove)
        {
            if (toRemove == null)
            {
                Console.WriteLine("MongoDbManager RemoveGame() passed a null value");
                return false;
            }

            MongoClient client = new MongoClient();
            var db = client.GetDatabase(
                ConfigurationManager.AppSettings["Database"]);
            var collection = db.GetCollection<Game>(
                ConfigurationManager.AppSettings["Collection"]);
            var result = collection.DeleteOne(_ => _.Id == toRemove.Id);

            return result.IsAcknowledged;
        }

        /// <summary>
        /// Runs a custom query using the find command on the MongoDB "Games" collection.
        /// </summary>
        /// <param name="query">Custom query string</param>
        /// <returns>List of games returned from query</returns>
        public static List<Game> RunCustomQuery(string query)
        {
            List<Game> queryResult = new List<Game>();

            try
            {
                // Create the BsonDocument used to run the query
                string collectionQuery = String.Format("{{find: \"{0}\"}}", ConfigurationManager.AppSettings["Collection"]);
                BsonDocument document = new BsonDocument();
                document.AddRange(MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>(collectionQuery));
                document.AddRange(TrimDocument(MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>(query)));
                
                // Run query and find the games within the resulting BsonDocument
                MongoClient client = new MongoClient();
                var db = client.GetDatabase("GameOrganizer");
                var result = db.RunCommand<BsonDocument>(document);

                // Results from find command stored in [cursor][firstBatch]
                queryResult = MongoDB.Bson.Serialization.BsonSerializer.Deserialize
                    <List<Game>>(result["cursor"]["firstBatch"].ToJson());
            }
            catch (MongoCommandException e)
            {
                Console.WriteLine(e);
            }
            catch(FormatException e)
            {
                Console.WriteLine(e);
                System.Windows.Forms.MessageBox.Show(
                    "The .json file was not formatted properly.",
                    "Error loading query file");
            }

            return queryResult;
        }

        /// <summary>
        /// Removes document elements not implemented in RunCustomQuery function. 
        /// </summary>
        /// <param name="toTrim">Original query document</param>
        /// <returns>New query document</returns>
        private static BsonDocument TrimDocument(BsonDocument toTrim)
        {
            BsonDocument newDocument = new BsonDocument();

            // Only add values for keys [filter, sort, limit, skip]
            if (toTrim.Contains("filter"))
            {
                newDocument.AddRange(MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>(
                    "{filter: " + toTrim["filter"].ToString() + "}"));
            }
            if (toTrim.Contains("sort"))
            {
                newDocument.AddRange(MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>(
                    "{sort: " + toTrim["sort"].ToString() + "}"));
            }
            if (toTrim.Contains("limit"))
            {
                newDocument.AddRange(MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>(
                    "{limit: " + toTrim["limit"].ToString() + "}"));
            }
            if (toTrim.Contains("skip"))
            {
                newDocument.AddRange(MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>(
                    "{skip: " + toTrim["skip"].ToString() + "}"));
            }

            return newDocument;
        }

        /// <summary>
        /// Updates the game with new values.
        /// </summary>
        /// <param name="previousValue">Previous game values</param>
        /// <param name="newValue">New game values</param>
        /// <returns>True if update successful</returns>
        public static bool UpdateGame(ObjectId gameId, Game newValue)
        {
            if (gameId == ObjectId.Empty || newValue == null)
            {
                Console.WriteLine("MongoDbManager UpdateGame() passed a null value");
                return false;
            }

            if(newValue.Id != ObjectId.Empty && gameId != newValue.Id)
            {
                Console.WriteLine("Cannot update game; object ids do not match.");
                return false;
            }

            MongoClient client = new MongoClient();
            var db = client.GetDatabase("GameOrganizer");
            var collection = db.GetCollection<Game>("Games");

            Game result = collection.FindOneAndReplace(a => a.Id == gameId, newValue);
            return result != null;
        }
    }
}

```
[Return to Homepage](https://davidmccannjr.github.io/ePortfolio/)
