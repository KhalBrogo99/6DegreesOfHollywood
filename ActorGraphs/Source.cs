using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMDbLib.Objects.Changes;
using TMDbLib.Objects.People;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Search;
using TMDbLib.Objects;
using TMDbLib.Client;
using TMDbLib.Utilities;
using TMDbLib.Objects.Movies;

namespace ActorGraphs
{
    class Source
    {
        static void Main(string[] args)
        {
            string APIkey = "0d2b5adffec3e19cd663b4cd400260f3";
            //Creating the client, big abstract methods are taken from this
            TMDbClient client = new TMDbClient(APIkey);
            System.Console.WriteLine("Please enter the name of an actor: ");

            //Taking an input and searching for the actor
            string toSearch = System.Console.ReadLine();

            //var is like the auto keyword in c++
            var searchedResult = client.SearchPersonAsync(toSearch).Result;
            
            bool correctSearch = false;
            int index = 0;
            var actor1 = client.GetPersonAsync(searchedResult.Results[index].Id, PersonMethods.MovieCredits).Result;
            while (!correctSearch)
            {
                
                Console.WriteLine("Did you mean: " + actor1.Name + "? In the department: " + actor1.KnownForDepartment + "? (y/n)");
                if (Console.ReadLine() == "y")
                    correctSearch = true;
                else
                    actor1 = client.GetPersonAsync(searchedResult.Results[++index].Id, PersonMethods.MovieCredits).Result;

            }

            System.Console.WriteLine("Please enter the name of a second actor: ");

            //Taking an input and searching for the actor
            string toSearch2 = System.Console.ReadLine();

            //var is like the auto keyword in c++
            var searchedResult2 = client.SearchPersonAsync(toSearch2).Result;

            correctSearch = false;
            index = 0;

            var actor2 = client.GetPersonAsync(searchedResult2.Results[index].Id, PersonMethods.MovieCredits).Result;
            while (!correctSearch)
            {
                Console.WriteLine("Did you mean: " + actor2.Name + "? In the department: " + actor2.KnownForDepartment + "? (y/n)");
                if (Console.ReadLine() == "y")
                    correctSearch = true;
                else
                    actor2 = client.GetPersonAsync(searchedResult2.Results[++index].Id, PersonMethods.MovieCredits).Result;
            }

            Graph g = new Graph(actor1, actor2, client);

            //Printing every actor that Henry Cavill has been in a movie with
            bool fun = g.DFS(client, actor1, actor2);

            fun = g.BFS(client, actor1, actor2);

            //From what I found online this is the only way to stop a C# console window from automatically closing
            System.Console.ReadLine();
        }

        
    }

}
