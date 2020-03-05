﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ML1
{
    public class Population
    {
        public bool[][] Individuals { get; protected set; }
        int itemCount;
        public double CrossoverRate { get; set; } = 0.9;

        double mutationRate = 0.005;
        public double MutationRate
        {
            get
            {
                return mutationRate;
            }
            set
            {
                mutationRate = value;
                mutationCount = (int)(itemCount * mutationRate);
            }
        }

        int mutationCount;



        public int BaseRouletteChance = 1;
        public int TournamentSize = 32;
        protected Task task;
        public Task Task
        {
            get
            {
                return task;
            }
            set
            {
                task = value ?? throw new NullReferenceException();
                itemCount = task.ItemCount;
            }
        }


        public string Description
        {
            get
            {
                return $"Population:" +
                    $"\n          Individuals: {$"{Individuals.Length}".PadLeft(6)}" +
                    $"\n Base roulette chance: {$"{BaseRouletteChance}".PadLeft(6)}" +
                    $"\n      Tournament size: {$"{TournamentSize}".PadLeft(6)}" +
                    $"\n        Mutation rate: {$"{MutationRate}".PadLeft(6)}" +
                    $"\n       Crossover rate: {$"{CrossoverRate}".PadLeft(6)}";
            }
        }

        protected Random rng;

        public Population(Task task, int populationSize, int seed)
        {
            Task = task;
            rng = new Random(seed);
            Individuals = new bool[populationSize][];
            for (int i = populationSize - 1; i >= 0; i--)
            {
                Individuals[i] = GetRandomIndividual(itemCount);
            }
        }

        bool[] GetRandomIndividual(int itemCount)
        {
            bool[] arr = new bool[itemCount];

            for (int i = itemCount - 1; i >= 0; i--)
            {
                arr[i] = rng.Next(6) == 0;
            }

            return arr;
        }

        protected (int, int) GetSizeAndWeight(int individualID)
        {
            if (task == null)
                throw new ArgumentNullException("Task");

            bool[] individual = Individuals[individualID];

            if (individual.Length != task.ItemCount)
                throw new ArgumentException("task.ItemCount and individual.Length");

            int totalSize = 0;
            int totalWeight = 0;

            for (int i = itemCount - 1; i >= 0; i--)
            {
                if (individual[i])
                {
                    totalSize += task.Items[i, 0];
                    totalWeight += task.Items[i, 1];
                }
            }
            return (totalSize, totalWeight);
        }

        public int Evaluate(int individualID)
        {
            (int, int) data = GetSizeAndWeight(individualID);

            if (data.Item1 > task.MaxSize || data.Item2 > task.MaxWeight)
                return 0;

            return data.Item1 + data.Item2;
        }

        public int EvaluateBest()
        {
            return Evaluate(BestID());
        }

        public bool[] Crossover(int parent1, int parent2)
        {
            if (parent1 >= Individuals.Length)
                throw new ArgumentOutOfRangeException("parent1");

            if (parent1 >= Individuals.Length)
                throw new ArgumentOutOfRangeException("parent2");

            bool[] child = new bool[itemCount];
            int crossoverPoint;
            if (Misc.rng.NextDouble() > CrossoverRate)
            {
                if (Misc.rng.Next(2) == 0)
                {
                    crossoverPoint = 0;
                }
                else
                {
                    crossoverPoint = itemCount;
                }
            }
            else
            {
                crossoverPoint = Misc.rng.Next(1, itemCount);
            }
           
            bool[] parent = Individuals[parent1];
            for (int i = 0; i < crossoverPoint && i < itemCount; i++)
            {
                child[i] = parent[i];
            }

            parent = Individuals[parent2];
            for (int i = crossoverPoint; i < itemCount; i++)
            {
                child[i] = parent[i];
            }
            return child;
        }

        public bool[] Mutate(bool[] individual)
        {
            if (individual == null)
                throw new ArgumentNullException("individual");

            int id;
            for (int i = 0; i < mutationCount; i++)
            {
                id = Misc.rng.Next(itemCount);
                individual[id] = !individual[id];
            }

            return individual;
        }

        public bool[] MutateTrue(bool[] individual)
        {
            if (individual == null)
                throw new ArgumentNullException("individual");

            int[] mutations = Misc.GetRandomUniqueIntegers(mutationCount, itemCount);
            for (int i = 0; i < mutationCount; i++)
            {

                individual[mutations[i]] = !individual[mutations[i]];
            }

            return individual;
        }

        protected int SingleTournament()
        {
            int[] contestants = Misc.GetRandomUniqueIntegers(TournamentSize, Individuals.Length);
            int bestFitness = 0;
            int bestId = 0;
            int tempFitness;
            for(int i = TournamentSize - 1; i >= 0; i--)
            {
                if((tempFitness = Evaluate(contestants[i])) > bestFitness)
                {
                    bestFitness = tempFitness;
                    bestId = contestants[i];
                }
            }
            return bestId;
        }

        public void Tournament()
        {
            bool[][] newPopulation = new bool[Individuals.Length][];
            for (int i = Individuals.Length - 1; i >= 0; i--)
            {
                newPopulation[i] = Mutate(Crossover(SingleTournament(), SingleTournament()));
            }
        }

        public void Roulette()
        {
            int[] fitnesses = new int[Individuals.Length];
            fitnesses[Individuals.Length - 1] = Evaluate(Individuals.Length - 1);
            
            for (int i = Individuals.Length - 2; i >= 0; i--)
            {
                fitnesses[i] = fitnesses[i+ 1] + Evaluate(i) / 1000 + BaseRouletteChance;
            }
            int parent1;
            int parent2;
            int parent1Point;
            int parent2Point;

            bool[][] newPopulation = new bool[Individuals.Length][];

            for (int i = Individuals.Length - 1; i >= 0; i--)
            {
                parent1 = -1;
                parent2 = -1;
                parent1Point = Misc.rng.Next(fitnesses[0]);
                
                while ((parent2Point = Misc.rng.Next(fitnesses[0])) == parent1Point) { }

                for(int j = Individuals.Length - 1; j >= 0 && (parent1 < 0 || parent2 < 0); j--)
                {
                    if (parent1 < 0 && parent1Point < fitnesses[j])
                        parent1 = j;
                    if (parent2 < 0 && parent2Point < fitnesses[j])
                        parent2 = j;
                }
                
                newPopulation[i] = Mutate(Crossover(parent1, parent2));
            }
            Individuals = newPopulation;
            
        }

        public void Show(int id)
        {
            for (int i = 0; i < itemCount; i++)
                Console.Write((Individuals[id][i] ? '#' : '.'));
            Console.WriteLine();
        }

        public void Show(bool[] indv)
        {
            for (int i = 0; i < indv.Length; i++)
                Console.Write((indv[i] ? '#' : '.'));
            Console.WriteLine();
        }

        public bool[] Best()
        {
            return (bool[])Individuals[BestID()].Clone();
        }

        public int BestID()
        {
            int bestFitness = 0;
            int bestId = 0;
            int tempFitness;
            for (int i = Individuals.Length - 1; i >= 0; i--)
            {
                if ((tempFitness = Evaluate(i)) > bestFitness)
                {
                    bestFitness = tempFitness;
                    bestId = i;
                }
            }
            return bestId;
        }

    }
}
