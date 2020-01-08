# Database Enhancement

## Narrative

  The “Who’s That Pokémon” guessing game project was the project I decided to enhance with a focus on Databases. This artifact was created because during the Pokémon television show, when heading into a commercial, the show would show a silhouette of a Pokémon and ask the viewer to guess it. This game uses a similar idea, where players must guess a randomly selected Pokémon and are given clues to steer them towards the answer. I created this project a couple of years ago, and I have continued to update the game to include new Pokémon information as new games have been released.
  
  The program is a simple but fun game that Pokémon fans can play, and the game utilizes the many programming skills that I have learned over the years. It utilizes a GUI with Windows Forms and the .NET framework, and originally it used external files to read in an manipulate the data. It also showcases my ability to plan, organize, develop, and maintain a project.
  
  My goal for this enhancement was to transfer the Pokemon information used in the project from an XML file to a MongoDB collection, which would allow for the information to be maintained easier while also giving me experience connecting to and retrieving information from a MongoDB database. Once the collection was created and the information was transferred, I updated the algorithms within the program to utilize the MongoDB rather than the XML file. In addition to this, I created a python script that utilized a RESTful API using the Bottle framework. Users can connect to the API, which can filter the list of possible Pokémon that the game has currently selected based on the hints that the game provides to the player after each guess. Using query strings in the URL path, a list of possible Pokémon would be returned.
  
  When enhancing this artifact, I learned how MongoDB is used for C# applications, which included information like how to add the necessary references and how to create a relationship between the MongoDB collection attributes and the C# class variables. I had some experience with using MongoDB from a class I took, but this was an opportunity to further gain experience with using MongoDB. 
  
  When creating the database query in the python application, I realized I had to learn how to combine the different queries and variables based on the input from the user. The API could filter the Pokémon list based on 5 variables: the Pokémon types, the Pokémon abilities, the generation it was introduced, the first letter of the name, and the length of the name. One specific challenge I faced was how to combine all of these possible queries into a single query. I originally found out PyMongo used dictionaries as queries, so I tried to add the queries to a dictionary, such as query[‘name’], but this didn’t work due to some queries requiring the “$and” operator, which couldn’t be combined with this syntax, and two queries being based on the ‘name’ attribute. In the end, I separated each specific query into their own dictionary and used the “$and” operator to combined the queries. It was a good learning experience researching and learning how to overcome these challenges, and now I feel more comfortable working with MongoDB in both a C# and Python environment.

![useful image]({{ site.url }}/ePortfolio/Images/WhosThatPokemon/QueryResult.png)

## Python Script

```python
#!/usr/bin/python

# WhosThatPokemon RESTful API program
# API that returns Pokemon information

#Current routes:
#   /WhosThatPokemon/api
#   /WhosThatPokemon/api/Help
#   /WhosThatPokemon/api/Pokemon
#   /WhosThatPokemon/api/Pokemon/<name>

import bottle
from bottle import redirect, request, route, run, abort
import pymongo
from pymongo import MongoClient

# MongoDB Connection
connection = MongoClient('localhost', 27017)
db = connection['WhosThatPokemon']
collection = db['Pokemon']

# Finds the pokemon with the given name.
def find_document(name):
  result = None
  try:
    result = collection.find_one({"name" : name}, {"_id" : 0})
  except Exception:
    abort(401, "Error occurred searching for Pokemon: " + name)
  
  return result

# Returns an html link to the /WhosThatPokemon/api/Pokemon/<name> route.
def name_to_link(name):
  if(name and name != ""):
    return "<a href = \"/WhosThatPokemon/api/Pokemon/" + name + "\">" + name +"</a>"
  return ""

# Creates a query based on the url parameters and returns the result of the query.
def send_query(types, abilities, gen, firstLetter, nameLength):
    result = None
    genQuery = {}
    firstQuery = {}
    lengthQuery = {}
    typeQuery = {}
    abilityQuery = {}

    if(gen):
      genQuery = {"gen" : gen}
    if(firstLetter):
      firstQuery = {"name" : {"$regex": '^' + firstLetter}}
    if(nameLength):
      lengthQuery = {"name" : {"$exists" : True}, "$expr" : {"$eq" : [{"$strLenCP" : "$name"}, nameLength]}}
    if(types):
        # Pokemon has the type in either type slot
        if(len(types) == 1):
            typeQuery = {"$or" : [{"type1": {"$in" : types}},
                                  {"type2" : {"$in" : types}}]}
        # Pokemon must have both types
        else:
            typeQuery = {"$and" : [{"type1": {"$in" : types}},
                                  {"type2" : {"$in" : types}}]}
    if abilities:
        # Pokemon has the ability in one of the ability slot
        if(len(abilities) == 1):
            abilityQuery = {"$or" : [{"ability1": {"$in" : abilities}},
                                     {"ability2" : {"$in" : abilities}},
                                     {"ability3" : {"$in" : abilities}}]}
        # Pokemon has two abilities in the ability slots
        if(len(abilities) == 2):
            abilityQuery = {"$or" : [{"$and" : [{"ability1": {"$in" : abilities}},
                                                {"ability2" : {"$in" : abilities}}]},
                                    {"$and" : [{"ability1": {"$in" : abilities}},
                                                {"ability3" : {"$in" : abilities}}]},
                                    {"$and" : [{"ability2": {"$in" : abilities}},
                                                {"ability3" : {"$in" : abilities}}]}]}
        # Pokemon has all three abilities
        if(len(abilities) == 3):
            abilityQuery = {"$and" : [{"ability1": {"$in" : abilities}},
                                     {"ability2" : {"$in" : abilities}},
                                     {"ability3" : {"$in" : abilities}}]}
    try:
        result = collection.find({"$and" : [genQuery,
                                            firstQuery,
                                            lengthQuery,
                                            typeQuery,
                                            abilityQuery]}, {"_id" : 0, "name" : 1})
    except Exception:
        abort(401, "Error sending query to database")
        
    return result

# Retrieve the Pokemon with the given name
@route('/WhosThatPokemon/api/Pokemon/<name>')
def get_pokemon(name):
  pokemon = find_document(name)
  if(not pokemon):
    return "Pokemon with name (" + name + ") does not exist"
  
  # Format Pokemon Information
  prettyPokemon = ""
  try:
    prettyPokemon += "Name: " + pokemon['name'] + "<br>"
    prettyPokemon += "Generation: " + str(pokemon['gen']) + "<br>"

    # Only display secondary typing if exists
    types = [pokemon['type1']]
    if(pokemon['type2'] != ''):
      types.append(pokemon['type2'])
    prettyPokemon += "Typing: " + ", ".join(types) + "<br>"

    # Only display multiple abilities if they exist
    abilities = [pokemon['ability1']]
    if(pokemon['ability2'] != ''):
       abilities.append(pokemon['ability2'])
    if(pokemon['ability3'] != ''):
       abilities.append(pokemon['ability3'])
    prettyPokemon += "Abilities: " + ", ".join(abilities) + "<br>"
    
    prettyPokemon += "Previous Evolution: " + name_to_link(pokemon['prevEvo']) + "<br>"
    prettyPokemon += "Next Evolution: " + name_to_link(pokemon['nextEvo']) + "<br>"
  except Exception(AttributeError, TypeError):
    abort(401, "Error occurred parsing pokemon information: " + name)
  
  return prettyPokemon

# Retrieve Pokemon with similar information
@route('/WhosThatPokemon/api/Pokemon')
def get_similar_pokemon():
    # Get query string information if it exists
    types = request.query.getlist('type')[:2] or None
    abilities = request.query.getlist('ability')[:3] or None
    gen = request.query.gen or None
    firstLetter = request.query.first or None
    nameLength = request.query.length or None
    
    # Ensure query information is valid
    if(gen and gen.isnumeric()):
      gen = int(gen)
    else:
      gen = None

    # Name length has to be numeric
    if(nameLength and nameLength.isnumeric()):
      nameLength = int(nameLength)
    else:
      nameLength = None

    # First letter has to be a single char and upper case
    if(firstLetter and len(firstLetter) > 0):
      firstLetter = firstLetter[0].upper()
    else:
      firstLetter = None
    
    result = send_query(types, abilities, gen, firstLetter, nameLength)
    
    names = ""
    if(result):
      for r in result.distinct("name"):
        names += name_to_link(r) + "<br>"
    return names

# Displays help page for the API
@route('/WhosThatPokemon/api/Help')
def help_page():
  return
"""
<pre>Welcome to the WhosThatPokemon API
This help page will help guide you through using the API
            
Search for a Pokemon using the '/WhosThatPokemon/api/Pokemon/<name>' url path
   Example: /WhosThatPokemon/api/Pokemon/Bulbasaur
               
Sending a Query
   Search by generation using 'gen' parameter (Gen 1 - Gen 7)               
      Example: /WhosThatPokemon/api/Pokemon?gen=1
                  
   Search by Pokemon types using 'type' parameter (2 types max)
      Example: /WhosThatPokemon/api/Pokemon?type=Fire
                  
   Search by Pokemon abilities using 'ability' parameter (3 abilities max)
      Example: /WhosThatPokemon/api/Pokemon?ability=Thick Fat
                  
   Search by name length using 'length' parameter
      Example: /WhosThatPokemon/api/Pokemon?length=5
                  
   Search by first letter using 'first' parameter
      Example: /WhosThatPokemon/api/Pokemon?first=A

                  
   Queries can have multiple search parameters
      Example : /WhosThatPokemon/api/Pokemon?first=C&gen=1&type=Fire&type=Flying</pre>
      """

# Redirects to help page
@route('/WhosThatPokemon/api')
def main_page():
  redirect("/WhosThatPokemon/api/Help")           

# Run the API
if __name__=='__main__':
  #app.run(debug=True)
  run(host='localhost', port=8080)

```

[Return to Homepage](https://davidmccannjr.github.io/ePortfolio/)
