using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Runtime.Serialization;
using System;

namespace GameOrganizer
{
    /// <summary>
    /// A class that stores information for a video game. Games have private
    /// constructors and must be initialized in the CreateGame function.
    /// </summary>
    [DataContract]
    public class Game
    {
        /// <summary>
        /// MongoDB object ID used to identify game in collection.
        /// </summary>
        [BsonId]
        [BsonIgnoreIfDefault]
        public ObjectId Id { get; private set; }

        /// <summary>
        /// Gets true if the game includes cover art.
        /// </summary>
        [DataMember]
        [BsonElement("hasCover")]
        public bool HasCover { get; private set; }

        /// <summary>
        /// Gets true if the game includes the manual.
        /// </summary>
        [DataMember]
        [BsonElement("hasManual")]
        public bool HasManual { get; private set; }

        /// <summary>
        /// Gets the player rating.
        /// </summary>
        [DataMember]
        [BsonElement("rating")]
        public int Rating { get; private set; }

        /// <summary>
        /// Gets the game genre.
        /// </summary>
        [DataMember]
        [BsonDefaultValue("Other")]
        [BsonElement("genre")]
        public string Genre { get; private set; }

        /// <summary>
        /// Gets the platform which the game was purchased.
        /// </summary>
        [DataMember]
        [BsonDefaultValue("Other")]
        [BsonElement("platform")]
        public string Platform { get; private set; }

        /// <summary>
        /// Gets the title of the game.
        /// </summary>
        [DataMember]
        [BsonDefaultValue("")]
        [BsonElement("title")]
        public string Title { get; private set; }

        /// <summary>
        /// Gets the purchase date.
        /// </summary>
        [DataMember]
        [BsonElement("purchased")]
        public DateTime Purchased { get; private set; }

        /// <summary>
        /// Gets the release date.
        /// </summary>
        [DataMember]
        [BsonElement("released")]
        public DateTime Released { get; private set; }

        /// <summary>
        /// Gets the purchase price.
        /// </summary>
        [DataMember]
        [BsonElement("cost")]
        public double Cost { get; private set; }

        /// <summary>
        /// Gets the cover art image path.
        /// </summary>
        [DataMember]
        [BsonDefaultValue("")]
        [BsonElement("coverFilePath")]
        public string CoverFileName { get; private set; }

        public string CoverFilePath
        {
            get
            {
                return Collection.BaseDirectory + "CoverArt\\" + CoverFileName;
            }
        }

        /// <summary>
        /// Private constructor for a video game.
        /// </summary>
        /// <param name="info">Game info</param>
        /// <param name="ownerInfo">Game ownership info</param>
        private Game(GameInfo info, OwnershipInfo ownerInfo)
        {
            HasCover = ownerInfo.HasCover;
            HasManual = ownerInfo.HasManual;
            Cost = ownerInfo.Cost;
            Rating = ownerInfo.Rating;
            CoverFileName = info.CoverFileName;
            Title = info.Title;
            Platform = info.Platform;
            Purchased = ownerInfo.Purchased;
            Released = info.Released;
            Genre = info.Genre;
        }

        /// <summary>
        /// Initializes and returns an instance of a Game object. If any parameter is
        /// invalid, returns null.
        /// </summary>
        /// <param name="info">Game info</param>
        /// <param name="ownerInfo">Game ownership info</param>
        /// <param name="coverFilePath">Path of the game's cover art file</param>
        /// <returns>An instance of the game.</returns>
        public static Game Create(GameInfo info, OwnershipInfo ownerInfo)
        {
            if (info == null || ownerInfo == null)
            {
                return null;
            }

            return new Game(info, ownerInfo);
        }
    }
}
