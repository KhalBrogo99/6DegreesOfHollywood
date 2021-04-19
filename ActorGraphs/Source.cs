using System;
using TMDbLib.Objects.People;
using TMDbLib.Client;

namespace ActorGraphs
{
    class Source
    {
        static void Main(string[] args)
        {
            bool rerun = true;
            Console.WriteLine("Welcome to 6 Degrees of Hollywood!\n");
            while (rerun)
            {
                rerun = Run();
                Console.WriteLine();
            }
            Console.WriteLine("\nGoodbye!");
            Console.ReadLine();
        }
        static bool Run()
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

            System.Console.WriteLine("\nPlease enter the name of a second actor: ");

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

            Console.WriteLine();
            Console.WriteLine("Creating graph between actors...");

            //create stopwatch
            var stopWatch = new System.Diagnostics.Stopwatch();

            stopWatch.Start();
            Graph g = new Graph(actor1, actor2, client);
            stopWatch.Stop();

            float graphTime = (float)stopWatch.ElapsedMilliseconds / 1000.2f / 60.2f;

            Console.WriteLine();
            Console.WriteLine("Time for graph population (min): " + graphTime.ToString("n2"));

            //timers for bfs and dfs
            long bfsTime = 0;
            long dfsTime = 0;

            //Reset stopwatch
            stopWatch.Reset();

            Console.WriteLine();

            //start stopwatch, stop after bfs is executed
            stopWatch.Start();
            Console.WriteLine("Executing using BFS...");
            bool fun = g.BFS(client, actor1, actor2);
            stopWatch.Stop();
            bfsTime = stopWatch.ElapsedTicks;

            //Reset stopwatch
            stopWatch.Reset();

            Console.WriteLine();

            //start stopwatch, stop after dfs is executed
            stopWatch.Start();
            Console.WriteLine("Executing using DFS...");
            var s = g.DFS(client, actor1, actor2);
            stopWatch.Stop();
            dfsTime = stopWatch.ElapsedTicks;

            //Unwind was originally called in the DFS program but was taken out
            //to directly compare the times for each traversal
            //This is why the stack is outputted in the DFS algorithm
            if (s.Count != 0)
                g.Unwind(s, client, actor1);

            //display timer results
            Console.WriteLine();
            Console.WriteLine("DFS elapsed time (ticks): " + dfsTime);
            Console.WriteLine("BFS elapsed time (ticks): " + bfsTime);

            Console.WriteLine();
            Console.WriteLine("Press R to restart, or any other key to end the program.");
            string restart = System.Console.ReadLine();
            if (restart.ToLower().Equals("r"))
                return true;
            else
                return false;

        }
    }
}