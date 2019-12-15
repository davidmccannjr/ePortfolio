using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace GameOrganizer
{
    /// <summary>
    /// User control that manages the container controls that sort
    /// and display the games within the game collection.
    /// </summary>
    public partial class ContainerManager : FlowLayoutPanel
    {
        /// <summary>
        /// Values in which the games are sorted
        /// </summary>
        public enum SortValue { Title, Platform, Genre, Released, Purchased, Rating, Cost, None }

        /// <summary>
        /// Returns true if any containers exist
        /// </summary>
        public bool HasContainers
        {
            get => containers.Count > 0;
        }

        /// <summary>
        /// Name for the catch-all container that stores the games that can't
        /// be catagorized in other containers due to special cases.
        /// </summary>
        private string CatchAllContainerName
        {
            get => "%";
        }

        /// <summary>
        /// List of container controls that organize game information
        /// </summary>
        private List<ContainerControl> containers;

        /// <summary>
        /// The current sorting function
        /// </summary>
        private Action<GameControl> sortBy;

        /// <summary>
        /// Initializes an empty list of containers that sort and display game information.
        /// </summary>
        public ContainerManager()
        {
            InitializeComponent();
            containers = new List<ContainerControl>();
            sortBy = SortByTitle;
        }

        /// <summary>
        /// Adds a container that stores game controls. The container
        /// rating corresponds to the game rating. A container must 
        /// contain a name and at least one game.
        /// </summary>
        /// <param name="rating">Game rating the container will hold</param>
        /// <param name="toSort">Game to add to the container.</param>
        private void AddRatingContainer(int rating, GameControl toSort)
        {
            string ratingStr = rating.ToString();

            if (toSort == null)
            {
                Console.WriteLine("No game found. Did not create a container of name " + ratingStr);
                return;
            }

            // Only create new container if container with the same name does not exist
            ContainerControl tempControl = containers.Find(container => container.Name == ratingStr);
            if (tempControl != null)
            {
                Console.WriteLine("Container with rating " + ratingStr + " already exists");
                tempControl.AddGame(toSort);
                return;
            }

            // Create a new container control to add to the container control list
            tempControl = new ContainerControl(ratingStr);
            tempControl.AddGame(toSort);

            // Insert container in sorted container list
            int insertIndex = containers.Count;
            for (int i = 0; i < containers.Count; i++)
            {
                if (Int32.TryParse(containers[i].Name, out int containerRating))
                {
                    if (rating > containerRating)
                    {
                        insertIndex = i;
                        break;
                    }
                }
            }
            containers.Insert(insertIndex, tempControl);

            // Add container to the GUI
            Controls.Add(tempControl);
            Controls.SetChildIndex(tempControl, insertIndex);
        }

        /// <summary>
        /// Adds a container that stores game controls. The container
        /// name corresponds to the sorting criteria. A container must 
        /// contain a name and at least one game.
        /// </summary>
        /// <param name="name">Name of the container.</param>
        /// <param name="toSort">Game to add to the container.</param>
        private void AddContainer(string name, GameControl toSort)
        {
            // Check for valid parameters before creating container
            if (name == null)
            {
                Console.WriteLine("Container must be given a name");
                return;
            }

            if (toSort == null)
            {
                Console.WriteLine("No game found. Did not create a container of name " + name);
                return;
            }

            // Only create new container if container with the same name does not exist
            ContainerControl tempControl = containers.Find(container => container.Name == name);
            if (tempControl != null)
            {
                Console.WriteLine("Container of name " + name + " already exists");
                tempControl.AddGame(toSort);
                return;
            }

            // Create a new container control to add to the container control list
            tempControl = new ContainerControl(name);
            tempControl.AddGame(toSort);

            // Insert container in sorted container list
            int insertIndex = containers.FindIndex(
                index => String.Compare(tempControl.Name, index.Name) == -1);

            if (insertIndex == -1)
            {
                containers.Add(tempControl);
            }
            else
            {
                containers.Insert(insertIndex, tempControl);
            }

            // Add container to the GUI
            Controls.Add(tempControl);
            Controls.SetChildIndex(tempControl, insertIndex);
        }

        /// <summary>
        /// Deletes a container. Any games currently
        /// stored in the container will also be removed.
        /// </summary>
        private void DeleteContainer(ContainerControl toDelete)
        {
            if (toDelete == null)
            {
                Console.WriteLine("DeleteContainer was passed a null value");
                return;
            }

            // Removes all games controls from container.
            toDelete.OpenContainer(false);
            while(toDelete.GameControls.Count > 0)
            {
                toDelete.RemoveGame(toDelete.GameControls[0]);
            }

            // Remove container from application
            containers.Remove(toDelete);
            Controls.Remove(toDelete);
        }

        /// <summary>
        /// Gets the corresponding sort function based on the passed sort value.
        /// </summary>
        /// <param name="newFunction">Sorting value</param>
        /// <returns>Sort function corresponding to the sort value</returns>
        private Action<GameControl> GetSortingFunc(SortValue sortValue)
        {
            switch (sortValue)
            {
                case SortValue.Platform:
                    return SortByPlatform;
                case SortValue.Genre:
                    return SortByGenre;
                case SortValue.Released:
                    return SortByRelease;
                case SortValue.Purchased:
                    return SortByPurchase;
                case SortValue.Cost:
                    return SortByCost;
                case SortValue.Rating:
                    return SortByRating;
                case SortValue.None:
                    return NoSort;
                case SortValue.Title:
                default:
                    return SortByTitle;
            }
        }

        /// <summary>
        /// Deletes the game from the game collection.
        /// </summary>
        /// <param name="toDelete">Game to delete</param>
        public void RemoveGame(GameControl toDelete)
        {
            if (toDelete == null)
            {
                Console.WriteLine("RemoveGame was passed a null value");
                return;
            }

            // Find the container where the game is stored.
            ContainerControl tempControl = containers.Find(a => a.GameControls.Contains(toDelete));
            if (tempControl != null)
            {
                tempControl.RemoveGame(toDelete);
                if (tempControl.GameControls.Count == 0)
                {
                    DeleteContainer(tempControl);
                }
            }
        }

        /// <summary>
        /// Removes all current containers and their corresponding games
        /// and then re-sorts the games based on a new sorting function.
        /// </summary>
        private void ReSortGames()
        {
            // Suspend layout for performance improvement
            SuspendLayout();

            // Get all current game controls before deleting current container
            List<GameControl> games = new List<GameControl>();
            while (containers.Count > 0)
            {
                games.AddRange(containers[0].GameControls);
                DeleteContainer(containers[0]);
            }

            // Re-sort the game controls
            foreach (var game in games)
            {
                sortBy(game);
            }

            // Resume layout after re-sorting games
            ResumeLayout();
        }

        /// <summary>
        /// Sets the sort function based on the given sort value.
        /// </summary>
        /// <param name="newSortValue">New sort value</param>
        public void SetSortFunction(SortValue newSortValue)
        {
            Action<GameControl> newSortBy = GetSortingFunc(newSortValue);

            // Only re-sort games if sorting function is different than previous
            if (newSortBy != null && sortBy != newSortBy)
            {
                sortBy = newSortBy;
                ReSortGames();
            }
        }

        /// <summary>
        /// Sorts a game into the appropriate container.
        /// </summary>
        /// <param name="toAdd">Game to add</param>
        public void SortGame(GameControl toAdd)
        {
            sortBy(toAdd);
        }

        /// <summary>
        /// Sorts the games into a single container.
        /// </summary>
        /// <param name="toSort">Game to sort</param>
        private void NoSort(GameControl toSort)
        {
            if (toSort == null || toSort.Game == null)
            {
                Console.WriteLine("SortByTitle passed a null value");
                return;
            }

            if (containers.Count == 0)
            {
                AddContainer("Games", toSort);
            }
            else
            {
                containers[0].AddGame(toSort);
            }
        }

        /// <summary>
        /// Sorts a game into a container based on the first letter in the title.
        /// </summary>
        /// <param name="toSort">Game to sort</param>
        private void SortByTitle(GameControl toSort)
        {
            if (toSort == null || toSort.Game == null)
            {
                Console.WriteLine("SortByTitle passed a null value");
                return;
            }

            // Use the first char in the title to organize the games.
            // If the first char is not a letter, set as the catch all char.
            string first = toSort.Game.Title.Length > 0 && Char.IsLetter(toSort.Game.Title[0]) ?
                toSort.Game.Title.Substring(0, 1).ToUpper() :
                CatchAllContainerName;

            // Look to see if an appropriate container already exists
            ContainerControl tempContainer = containers.Find(a => a.Name.Substring(0, 1) == first);
            if (tempContainer != null)
            {
                tempContainer.AddGame(toSort);
            }
            else
            {
                AddContainer(first, toSort);
            }
        }

        /// <summary>
        /// Sorts a game into a container based on the platform.
        /// </summary>
        /// <param name="toSort">Game to sort</param>
        private void SortByPlatform(GameControl toSort)
        {
            if (toSort == null || toSort.Game == null)
            {
                Console.WriteLine("SortByPlatform passed a null value");
                return;
            }

            // Look to see if an appropriate container already exists
            ContainerControl tempContainer = containers.Find(a => a.Name == toSort.Game.Platform);
            if (tempContainer != null)
            {
                tempContainer.AddGame(toSort);
            }
            else
            {
                AddContainer(toSort.Game.Platform, toSort);
            }
        }

        /// <summary>
        /// Sorts a game into a container  based on the genre.
        /// </summary>
        /// <param name="toSort">Game to sort</param>
        private void SortByGenre(GameControl toSort)
        {
            if (toSort == null || toSort.Game == null)
            {
                Console.WriteLine("SortByGenre passed a null value");
                return;
            }

            // Look to see if an appropriate container already exists
            ContainerControl c = containers.Find(a => a.Name == toSort.Game.Genre);
            if (c != null)
            {
                c.AddGame(toSort);
            }
            else
            {
                AddContainer(toSort.Game.Genre, toSort);
            }
        }

        /// <summary>
        /// Sorts a game into a container based on the release date.
        /// </summary>
        /// <param name="toSort">Game to sort</param>
        private void SortByRelease(GameControl toSort)
        {
            if (toSort == null || toSort.Game == null)
            {
                Console.WriteLine("SortByRelease passed a null value");
                return;
            }

            // Look to see if an appropriate container already exists
            ContainerControl c = containers.Find(a => a.Name == toSort.Game.Released.Year.ToString());
            if (c != null)
            {
                c.AddGame(toSort);
            }
            else
            {
                AddContainer(toSort.Game.Released.Year.ToString(), toSort);
            }
        }

        /// <summary>
        /// Sorts a game into a container based on the purchase date.
        /// </summary>
        /// <param name="toSort">Game to sort</param>
        private void SortByPurchase(GameControl toSort)
        {
            if (toSort == null || toSort.Game == null)
            {
                Console.WriteLine("SortByPurchase passed a null value");
                return;
            }

            // Look to see if an appropriate container already exists
            ContainerControl c = containers.Find(a => a.Name == toSort.Game.Purchased.Year.ToString());
            if (c != null)
            {
                c.AddGame(toSort);
            }
            else
            {
                AddContainer(toSort.Game.Purchased.Year.ToString(), toSort);
            }
        }

        /// <summary>
        /// Sorts a game into a container based on the cost.
        /// </summary>
        /// <param name="toSort">Game to sort</param>
        private void SortByCost(GameControl toSort)
        {
            if (toSort == null || toSort.Game == null)
            {
                Console.WriteLine("SortByCost passed a null value");
                return;
            }

            // Get range of values (0 - 9.99, increasing by 10)
            double cost = Convert.ToDouble(toSort.Game.Cost);
            double floor = cost <= 0 ? 0 : Math.Floor(cost / 10) * 10;
            double ceil = cost <= 0 ? 9.99 : Math.Ceiling((cost + 0.01) / 10) * 10 - 0.01;
            string range = "$" + floor + " - $" + ceil;

            // Look to see if an appropriate container already exists
            ContainerControl c = containers.Find(a => a.Name == range);
            if (c != null)
            {
                c.AddGame(toSort);
            }
            else
            {
                AddContainer(range, toSort);
            }
        }

        /// <summary>
        /// Sorts a game into a container based on the rating.
        /// </summary>
        /// <param name="toSort">Game to sort</param>
        private void SortByRating(GameControl toSort)
        {
            if (toSort == null || toSort.Game == null)
            {
                Console.WriteLine("SortByRating passed a null value");
                return;
            }

            // Look to see if an appropriate container already exists
            ContainerControl c = containers.Find(a => a.Name == toSort.Game.Rating.ToString());
            if (c != null)
            {
                c.AddGame(toSort);
            }
            else
            {
                AddRatingContainer(toSort.Game.Rating, toSort);
            }
        }
    }
}