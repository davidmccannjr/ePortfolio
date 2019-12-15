using System;
using System.Collections.Generic;
using System.Text;

namespace GameOrganizer
{
    /// <summary>
    /// A class that stores information for a video game. Games have private
    /// constructors and must be initialized in the CreateGame function.
    /// </summary>
    public class Game : IDisposable, IEquatable<Game>
    {
        #region Variables

        /// <summary>
        /// Flag: Has Dispose already been called? 
        /// </summary>
        private bool disposed = false;

        /// <summary>
        /// Gets the cover art bitmap.
        /// </summary>
        public readonly System.Drawing.Image    CoverArt;

        /// <summary>
        /// Information about the game.
        /// </summary>
        private readonly GameInfo                Info;

        /// <summary>
        /// Information about the ownership of the game.
        /// </summary>
        private readonly OwnershipInfo           OwnerInfo;

        #endregion

        #region Properties

        /// <summary>
        /// Gets true if the game includes cover art.
        /// </summary>
        public bool HasCover { get { return OwnerInfo.HasCover; } }

        /// <summary>
        /// Gets true if the game includes the manual.
        /// </summary>
        public bool HasManual { get { return OwnerInfo.HasManual; } }

        /// <summary>
        /// Gets the player rating.
        /// </summary>
        public byte Rating { get { return OwnerInfo.Rating; } }

        /// <summary>
        /// Gets the game genre.
        /// </summary>
        public string Genre { get { return Info.Genre; } }

        /// <summary>
        /// Gets the platform which the game was purchased.
        /// </summary>
        public string Platform { get { return Info.Platform; } }

        /// <summary>
        /// Gets the title of the game.
        /// </summary>
        public string Title { get { return Info.Title; } }

        /// <summary>
        /// Gets the purchase date.
        /// </summary>
        public DateTime Purchased { get { return OwnerInfo.Purchased; } }

        /// <summary>
        /// Gets the release date.
        /// </summary>
        public DateTime Released { get { return Info.Released; } }

        /// <summary>
        /// Gets the purchase price.
        /// </summary>
        public double Cost { get { return OwnerInfo.Cost; } }

        /// <summary>
        /// Gets the cover art image path.
        /// </summary>
        public string CoverFilePath { get { return Info.CoverFilePath; } }

        #endregion

        #region Functions

        /// <summary>
        /// Private constructor for a video game.
        /// </summary>
        /// <param name="info">Game info</param>
        /// <param name="ownerInfo">Game ownership info</param>
        private Game(GameInfo info, OwnershipInfo ownerInfo) 
        {
            Info      = info;
            OwnerInfo = ownerInfo;

            try 
            {
                if (CoverFilePath != "")
                {
                    CoverArt = new System.Drawing.Bitmap(CoverFilePath);
                }
            }
            catch 
            {
                CoverArt = null;
            }
        }

        /// <summary>
        /// Initializes and returns an instance of a Game object. If any parameter is
        /// invalid, returns null.
        /// </summary>
        /// <param name="info">Game info</param>
        /// <param name="ownerInfo">Game ownership info</param>
        /// <param name="coverFilePath">Path of the game's cover art file</param>
        /// <returns>An instance of the game.</returns>
        public static Game Build(GameInfo info, OwnershipInfo ownerInfo)
        {
            if (info == null || ownerInfo == null) return null;
            
            return new Game(info, ownerInfo);
        }

        /// <summary>
        /// Public implementation of Dispose pattern.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

       /// <summary>
       /// Protected implementation of Dispose pattern. 
       /// </summary>
       /// <param name="disposing">Is disposing</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                //CoverArt.Dispose();
            }
            disposed = true;
        }

        /// <summary>
        /// Checks if two games are equal.
        /// A game's unique identifiers are its title and platform.
        /// </summary>
        /// <param name="other">Other game</param>
        /// <returns>Comparison value</returns>
        public bool Equals(Game other)
        {
            return Title == other.Title && 
                    Platform == other.Platform;
        }

        #endregion
    }
}
