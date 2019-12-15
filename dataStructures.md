# Data Structures / Algorithms

## Narrative

The "Video Game Collection" project was the project I also decided to enhance with a focus on data structures and algorithms. My artifact is a personal project of mine from a few years ago that displays the video games that I currently own. For my Software Design / Engineering enhancement I showcased my ability to alter a project while still maintaining its functionality, as I altered how the program read in the collection data. For this enhancement, however, the enhancements I made showcase my ability to recognize ways to improve a program and implement those improvements into the project. 

When viewing the game collection, I only included ways to sort the collection based on the game attributes such as the game platform or game title. While these sorting options would sort and display the whole game collection, there was no way for a user to find subsets of games within the collection. As an example, users could sort the collection based on the year the game was purchased, but there was no way for them to find the games purchased between two different dates. As an enhancement to the project, this week I added an option for the users to create and view the results of a custom query by loading the custom query through a “json” file. This was possible due to the enhancements I made previously on the project, where I implemented a MongoDb collection to store the games in the collection.

I completed the course objectives I set for myself during the week I worked on this enhancement. I added the ability for users to load and view the results of a custom query, while also attempting to make sure the custom query being loaded was safe. As a security measure, I made sure the query only ran on a specific collection within the database and only utilized specific query options.   
  
The challenges I faced this week were taking MongoDb style queries and trying to implement them using the C# MongoDb driver, which I think I accomplished in a clean and concise manner. When working on the enhancement, I had to figure out how I wanted the custom query to be loaded into the program. I learned from researching that I couldn’t simply write a MongoDb query and basically run the query in a one-to-one transaction. The MongoDB driver Find() function in C# utilizes LINQ-style syntax, so I would have to really manipulate the custom query string to work with that syntax. Instead, I found that, like with MongoDb, the C# driver also utilizes the db.runCommand() function that uses documents to run a query, with the syntax being identical to how MongoDb queries run the same function. 
  
Once learning of this method, I decided to load in JSON files which I could easily deserialize to a BsonDocument object, which I could then use to run the custom query. As stated before, as a security measure I only pulled the appropriate information from the BsonDocument that could be used in the db.runCommand() query, and I manually input the collection filter into the document from the code so that I knew the query was running on the specific collection. I learned the the db.runCommand() command would return a BsonDocument, and that the results are stored in the [cursor][firstBatch] index of the BsonDocument. Once I learned this, I could take the result, find the elements, and deserialize them into a list of games, which I then returned and displayed in the custom query form. I had to go online to search for answers for a lot of questions, but thankfully there are a lot of questions with help and feedback that proved invaluable in solving these challenges. 

## Custom Query Form Class

'''c#

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;
using System.Drawing;

namespace GameOrganizer
{
    public partial class CustomQueryForm : Form
    {
        /// <summary>
        /// Current selected game control
        /// </summary>
        private GameControl selected;

        /// <summary>
        /// The query currently being displayed
        /// </summary>
        private string currentQuery;

        /// <summary>
        /// Initiates a new Custom Query Form
        /// </summary>
        public CustomQueryForm()
        {
            InitializeComponent();

            returnCountLabel.Text = "0";
            returnCostLabel.Text = "$0.00";
            viewQueryButton.Enabled = false;
            currentQuery = "";

            // Set sort function to None so all results grouped together
            containerManager.SetSortFunction(ContainerManager.SortValue.None);
        }

        /// <summary>
        /// Displays the results of the custom query. The query file must be a .json file.
        /// </summary>
        /// <param name="queryFileName">The .json query file</param>
        private void LoadNewQuery(string queryFileName)
        {
            using (StreamReader r = new StreamReader(queryFileName))
            {
                currentQuery = r.ReadToEnd();
            }

            // Get the result of the custom query
            List<Game> games = MongoDbManager.RunCustomQuery(currentQuery);
            if (games != null)
            {
                double cost = 0;
                containerManager.SuspendLayout();
                foreach (var game in games)
                {
                    if (game == null)
                    {
                        continue;
                    }

                    // Create a new game control
                    GameControl control = new GameControl(game);
                    control.Click += GameControl_Click;
                    containerManager.SortGame(control);

                    // Increase the total cost
                    cost += game.Cost;
                }
                containerManager.ResumeLayout();

                // Set the stat label information
                returnCountLabel.Text = games.Count.ToString();
                returnCostLabel.Text = cost.ToString("$0.00");
                viewQueryButton.Enabled = true;
            }
        }

        /// <summary>
        /// Removes the previous query results from the form
        /// </summary>
        private void RemoveQueryResults()
        {
            returnCountLabel.Text = "0";
            returnCostLabel.Text = "$0.00";
            viewQueryButton.Enabled = false;
            currentQuery = "";
            ResetSelected();

            // Remove click event from each game control before clearing the game controls
            containerManager.SuspendLayout();
            if (containerManager.HasContainers)
            {
                foreach (Control control in containerManager.Controls)
                {
                    if (control is ContainerControl containerControl)
                    {
                        while(containerControl.GameControls.Count > 0)
                        {
                            containerControl.OpenContainer(false);
                            if (containerControl.GameControls[0] == null)
                            {
                                continue;
                            }
                            containerControl.GameControls[0].Click -= GameControl_Click;
                            containerManager.RemoveGame(containerControl.GameControls[0]);
                        }
                    }
                }
            }
            containerManager.Controls.Clear();
            containerManager.ResumeLayout();
        }

        /// <summary>
        /// Unselects the current selected game and resets display information.
        /// </summary>
        private void ResetSelected()
        {
            // First reset background color before unselecting
            SetSelectedColor(GameControl.DefaultColor);

            selected = null;
            gameDisplay1.ResetDisplay();
        }

        /// <summary>
        /// Unselects the current selected game and resets display information.
        /// </summary>
        private void SetSelected(GameControl gC)
        {
            if (gC == null || gC.Game == null)
            {
                return;
            }

            // If there is a selected game control, change background color to default
            SetSelectedColor(GameControl.DefaultColor);

            // Set selected to the new game control and highlight
            selected = gC;
            SetSelectedColor(GameControl.HighlightColor);

            // Display selected game information
            gameDisplay1.Display(gC.Game);
        }

        /// <summary>
        /// Sets the background color of the selected game control.
        /// </summary>
        /// <param name="color"></param>
        private void SetSelectedColor(Color color)
        {
            // Set background color back to default
            if (selected != null)
            {
                selected.BackColor = color;
            }
        }

        /// <summary>
        /// Delegate called when a game control is selected.
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Game Control args</param>
        private void GameControl_Click(object sender, EventArgs e)
        {
            if (!(sender is GameControl gC))
            {
                return;
            }
            
            SetSelected(gC);
        }

        /// <summary>
        /// Delegate called when the new query menu item is clicked.
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Event args</param>
        private void NewQueryMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "JSON Files(*.json) | *.json";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    RemoveQueryResults();
                    LoadNewQuery(dialog.FileName);
                }
            }
        }

        /// <summary>
        /// Delegate called when the help tool strip item is clicked.
        /// </summary>
        private void HelpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var helpForm = new QueryHelpForm())
            {
                helpForm.ShowDialog();
            }
        }

        /// <summary>
        /// Delegate called when the view query button is clicked.
        /// </summary>
        private void ViewQueryButton_Click(object sender, EventArgs e)
        {
            MessageBox.Show(currentQuery, "Custom Query");
        }

        /// <summary>
        /// Delegate called when the form is closed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CustomQueryForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            // Remove query results before closing to free resources
            RemoveQueryResults();
        }
    }
}

'''
