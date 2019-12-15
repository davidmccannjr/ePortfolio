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
