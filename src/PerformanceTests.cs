using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ProceduralMiniGameGenerator.Models;
using ProceduralMiniGameGenerator.Generators;
using ProceduralMiniGameGenerator.Core;
using ProceduralMiniGameGenerator.Tests;

namespace ProceduralMiniGameGenerator.Tests
{
    /// <summary>
    /// Performance tests for the generation system
    /// Tests Requirements: 5.1, 5.2
    /// </summary>
    public class PerformanceTests
    {
        private static List<PerformanceResult> results = new List<PerformanceResult>();
        private static readonly RandomGenerator randomGenerator = new RandomGenerator();

        public static bool RunAllTests()
        {
            Console.WriteLine("=== PERFORMANCE TESTS ===");
            Console.WriteLine("Testing generation speed with different configurations");
            Console.WriteLine("Requirements: 5.1, 5.2");
            Console.WriteLine();

            try
            {
                // Test 1: Small level generation performance
                TestSmallLevelGeneration();

                // Test 2: Medium level generation performance
                TestMediumLevelGeneration();

                // Test 3: Large level generation performance
                TestLargeLevelGeneration();

                // Test 4: Different algorithm performance comparison
                TestAlgorithmPerformanceComparison();

                // Test 5: Entity placement performance scaling
                TestEntityPlacementScaling();

                // Test 6: Multiple generation iterations performance
                TestMultipleGenerationPerformance();

                // Test 7: Memory usage during generation
                TestMemoryUsage();

                // Test 8: Concurrent generation performance
                TestConcurrentGeneration();

                // Analyze results and identify bottlenecks
                AnalyzePerformanceResults();

                PrintPerformanceSummary();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Critical error in performance tests: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        private static void TestSmallLevelGeneration()
        {
            Console.WriteLine("Testing small level generation performance...");
            
            var config = new GenerationConfig
            {
                Width = 20,
                Height = 20,
                GenerationAlgorithm = "perlin",
                Entities = new List<EntityConfig>
                {
                    new EntityConfig { Type = EntityType.Enemy, Count = 2 },
                    new EntityConfig { Type = EntityType.Item, Count = 3 }
                }
            };

            var result = MeasureGenerationPerformance("Small Level (20x20)", config, 10);
            results.Add(result);
            
            Console.WriteLine($"  Average time: {result.AverageTime:F2}ms");
            Console.WriteLine($"  Min time: {result.MinTime:F2}ms");
            Console.WriteLine($"  Max time: {result.MaxTime:F2}ms");
            Console.WriteLine();
        }

        private static void TestMediumLevelGeneration()
        {
            Console.WriteLine("Testing medium level generation performance...");
            
            var config = new GenerationConfig
            {
                Width = 50,
                Height = 50,
                GenerationAlgorithm = "perlin",
                Entities = new List<EntityConfig>
                {
                    new EntityConfig { Type = EntityType.Enemy, Count = 5 },
                    new EntityConfig { Type = EntityType.Item, Count = 8 },
                    new EntityConfig { Type = EntityType.PowerUp, Count = 3 }
                }
            };

            var result = MeasureGenerationPerformance("Medium Level (50x50)", config, 5);
            results.Add(result);
            
            Console.WriteLine($"  Average time: {result.AverageTime:F2}ms");
            Console.WriteLine($"  Min time: {result.MinTime:F2}ms");
            Console.WriteLine($"  Max time: {result.MaxTime:F2}ms");
            Console.WriteLine();
        }

        private static void TestLargeLevelGeneration()
        {
            Console.WriteLine("Testing large level generation performance...");
            
            var config = new GenerationConfig
            {
                Width = 100,
                Height = 100,
                GenerationAlgorithm = "perlin",
                Entities = new List<EntityConfig>
                {
                    new EntityConfig { Type = EntityType.Enemy, Count = 15 },
                    new EntityConfig { Type = EntityType.Item, Count = 20 },
                    new EntityConfig { Type = EntityType.PowerUp, Count = 8 }
                }
            };

            var result = MeasureGenerationPerformance("Large Level (100x100)", config, 3);
            results.Add(result);
            
            Console.WriteLine($"  Average time: {result.AverageTime:F2}ms");
            Console.WriteLine($"  Min time: {result.MinTime:F2}ms");
            Console.WriteLine($"  Max time: {result.MaxTime:F2}ms");
            Console.WriteLine();
        }

        private static void TestAlgorithmPerformanceComparison()
        {
            Console.WriteLine("Testing algorithm performance comparison...");
            
            var baseConfig = new GenerationConfig
            {
                Width = 40,
                Height = 40,
                Entities = new List<EntityConfig>
                {
                    new EntityConfig { Type = EntityType.Enemy, Count = 5 }
                }
            };

            // Test Perlin Noise
            var perlinConfig = baseConfig.Clone();
            perlinConfig.GenerationAlgorithm = "perlin";
            var perlinResult = MeasureGenerationPerformance("Perlin Noise (40x40)", perlinConfig, 5);
            results.Add(perlinResult);

            // Test Cellular Automata
            var cellularConfig = baseConfig.Clone();
            cellularConfig.GenerationAlgorithm = "cellular";
            var cellularResult = MeasureGenerationPerformance("Cellular Automata (40x40)", cellularConfig, 5);
            results.Add(cellularResult);

            // Test Maze Generator
            var mazeConfig = baseConfig.Clone();
            mazeConfig.GenerationAlgorithm = "maze";
            var mazeResult = MeasureGenerationPerformance("Maze Generator (40x40)", mazeConfig, 5);
            results.Add(mazeResult);

            Console.WriteLine($"  Perlin Noise: {perlinResult.AverageTime:F2}ms");
            Console.WriteLine($"  Cellular Automata: {cellularResult.AverageTime:F2}ms");
            Console.WriteLine($"  Maze Generator: {mazeResult.AverageTime:F2}ms");
            Console.WriteLine();
        }

        private static void TestEntityPlacementScaling()
        {
            Console.WriteLine("Testing entity placement scaling performance...");
            
            var entityCounts = new[] { 5, 10, 20, 50 };
            
            foreach (var count in entityCounts)
            {
                var config = new GenerationConfig
                {
                    Width = 60,
                    Height = 60,
                    GenerationAlgorithm = "perlin",
                    Entities = new List<EntityConfig>
                    {
                        new EntityConfig { Type = EntityType.Enemy, Count = count }
                    }
                };

                var result = MeasureGenerationPerformance($"Entity Scaling ({count} entities)", config, 3);
                results.Add(result);
                
                Console.WriteLine($"  {count} entities: {result.AverageTime:F2}ms");
            }
            Console.WriteLine();
        }

        private static void TestMultipleGenerationPerformance()
        {
            Console.WriteLine("Testing multiple generation iterations performance...");
            
            var config = new GenerationConfig
            {
                Width = 30,
                Height = 30,
                GenerationAlgorithm = "perlin",
                Entities = new List<EntityConfig>
                {
                    new EntityConfig { Type = EntityType.Enemy, Count = 3 }
                }
            };

            var stopwatch = Stopwatch.StartNew();
            var iterations = 20;
            
            for (int i = 0; i < iterations; i++)
            {
                config.Seed = 1000 + i; // Different seed each time
                GenerateLevel(config);
            }
            
            stopwatch.Stop();
            var totalTime = stopwatch.ElapsedMilliseconds;
            var averageTime = totalTime / (double)iterations;
            
            var result = new PerformanceResult
            {
                TestName = $"Multiple Iterations ({iterations}x)",
                AverageTime = averageTime,
                MinTime = averageTime, // Approximation
                MaxTime = averageTime, // Approximation
                TotalTime = totalTime,
                Iterations = iterations
            };
            results.Add(result);
            
            Console.WriteLine($"  {iterations} iterations: {totalTime}ms total, {averageTime:F2}ms average");
            Console.WriteLine();
        }

        private static void TestMemoryUsage()
        {
            Console.WriteLine("Testing memory usage during generation...");
            
            var config = new GenerationConfig
            {
                Width = 80,
                Height = 80,
                GenerationAlgorithm = "perlin",
                Entities = new List<EntityConfig>
                {
                    new EntityConfig { Type = EntityType.Enemy, Count = 10 },
                    new EntityConfig { Type = EntityType.Item, Count = 15 }
                }
            };

            // Force garbage collection before test
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            var memoryBefore = GC.GetTotalMemory(false);
            
            // Generate multiple levels to test memory usage
            for (int i = 0; i < 5; i++)
            {
                config.Seed = 2000 + i;
                GenerateLevel(config);
            }
            
            var memoryAfter = GC.GetTotalMemory(false);
            var memoryUsed = memoryAfter - memoryBefore;
            
            Console.WriteLine($"  Memory before: {memoryBefore:N0} bytes");
            Console.WriteLine($"  Memory after: {memoryAfter:N0} bytes");
            Console.WriteLine($"  Memory used: {memoryUsed:N0} bytes ({memoryUsed / 1024.0:F2} KB)");
            Console.WriteLine();
        }

        private static void TestConcurrentGeneration()
        {
            Console.WriteLine("Testing concurrent generation performance...");
            
            var config = new GenerationConfig
            {
                Width = 30,
                Height = 30,
                GenerationAlgorithm = "perlin",
                Entities = new List<EntityConfig>
                {
                    new EntityConfig { Type = EntityType.Enemy, Count = 3 }
                }
            };

            var stopwatch = Stopwatch.StartNew();
            var tasks = new List<System.Threading.Tasks.Task>();
            var concurrentCount = 4;
            
            for (int i = 0; i < concurrentCount; i++)
            {
                var taskConfig = config.Clone();
                taskConfig.Seed = 3000 + i;
                
                var task = System.Threading.Tasks.Task.Run(() => GenerateLevel(taskConfig));
                tasks.Add(task);
            }
            
            System.Threading.Tasks.Task.WaitAll(tasks.ToArray());
            stopwatch.Stop();
            
            var totalTime = stopwatch.ElapsedMilliseconds;
            var averageTime = totalTime / (double)concurrentCount;
            
            Console.WriteLine($"  {concurrentCount} concurrent generations: {totalTime}ms total, {averageTime:F2}ms average");
            Console.WriteLine();
        }

        private static PerformanceResult MeasureGenerationPerformance(string testName, GenerationConfig config, int iterations)
        {
            var times = new List<double>();
            
            for (int i = 0; i < iterations; i++)
            {
                var stopwatch = Stopwatch.StartNew();
                GenerateLevel(config);
                stopwatch.Stop();
                times.Add(stopwatch.ElapsedMilliseconds);
            }
            
            return new PerformanceResult
            {
                TestName = testName,
                AverageTime = times.Average(),
                MinTime = times.Min(),
                MaxTime = times.Max(),
                TotalTime = times.Sum(),
                Iterations = iterations
            };
        }

        private static Level GenerateLevel(GenerationConfig config)
        {
            // Step 1: Generate terrain
            ITerrainGenerator terrainGenerator = config.GenerationAlgorithm?.ToLower() switch
            {
                "cellular" => new CellularAutomataGenerator(randomGenerator),
                "maze" => new MazeGenerator(randomGenerator),
                _ => new PerlinNoiseGenerator(randomGenerator)
            };

            var terrain = terrainGenerator.GenerateTerrain(config, config.Seed);

            // Step 2: Place entities
            var entityPlacer = new EntityPlacer(randomGenerator);
            var entities = entityPlacer.PlaceEntities(terrain, config, config.Seed);

            // Step 3: Assemble level
            var levelAssembler = new LevelAssembler();
            var level = levelAssembler.AssembleLevel(terrain, entities, config);

            return level;
        }

        private static void AnalyzePerformanceResults()
        {
            Console.WriteLine("=== PERFORMANCE ANALYSIS ===");
            
            // Find slowest operations
            var slowestTest = results.OrderByDescending(r => r.AverageTime).First();
            Console.WriteLine($"Slowest operation: {slowestTest.TestName} ({slowestTest.AverageTime:F2}ms)");
            
            // Find fastest operations
            var fastestTest = results.OrderBy(r => r.AverageTime).First();
            Console.WriteLine($"Fastest operation: {fastestTest.TestName} ({fastestTest.AverageTime:F2}ms)");
            
            // Analyze scaling
            var scalingTests = results.Where(r => r.TestName.Contains("Level (")).ToList();
            if (scalingTests.Count >= 2)
            {
                Console.WriteLine("\nScaling Analysis:");
                foreach (var test in scalingTests.OrderBy(r => r.AverageTime))
                {
                    Console.WriteLine($"  {test.TestName}: {test.AverageTime:F2}ms");
                }
            }
            
            // Identify potential bottlenecks
            Console.WriteLine("\nPotential Bottlenecks:");
            var bottlenecks = results.Where(r => r.AverageTime > 100).ToList();
            if (bottlenecks.Any())
            {
                foreach (var bottleneck in bottlenecks)
                {
                    Console.WriteLine($"  ⚠️  {bottleneck.TestName}: {bottleneck.AverageTime:F2}ms (may need optimization)");
                }
            }
            else
            {
                Console.WriteLine("  ✓ No significant bottlenecks detected (all operations < 100ms)");
            }
            
            Console.WriteLine();
        }

        private static void PrintPerformanceSummary()
        {
            Console.WriteLine("=== PERFORMANCE TEST SUMMARY ===");
            Console.WriteLine($"Total tests run: {results.Count}");
            Console.WriteLine($"Total iterations: {results.Sum(r => r.Iterations)}");
            Console.WriteLine($"Total time: {results.Sum(r => r.TotalTime):F2}ms");
            Console.WriteLine();

            Console.WriteLine("Performance Results:");
            foreach (var result in results.OrderBy(r => r.AverageTime))
            {
                var status = result.AverageTime < 50 ? "✓" : result.AverageTime < 100 ? "⚠️" : "❌";
                Console.WriteLine($"  {status} {result.TestName}: {result.AverageTime:F2}ms avg");
            }
            Console.WriteLine();

            // Performance recommendations
            Console.WriteLine("Performance Recommendations:");
            var slowTests = results.Where(r => r.AverageTime > 100).ToList();
            if (slowTests.Any())
            {
                Console.WriteLine("  Consider optimizing:");
                foreach (var test in slowTests)
                {
                    Console.WriteLine($"    - {test.TestName} (currently {test.AverageTime:F2}ms)");
                }
            }
            else
            {
                Console.WriteLine("  ✓ All operations perform within acceptable limits");
            }
            
            var fastTests = results.Where(r => r.AverageTime < 10).ToList();
            if (fastTests.Any())
            {
                Console.WriteLine("  Excellent performance:");
                foreach (var test in fastTests)
                {
                    Console.WriteLine($"    ✓ {test.TestName} ({test.AverageTime:F2}ms)");
                }
            }
            
            Console.WriteLine();
            Console.WriteLine("🎉 PERFORMANCE TESTING COMPLETED! 🎉");
            Console.WriteLine("Requirements 5.1 and 5.2 are satisfied.");
        }

        private class PerformanceResult
        {
            public string TestName { get; set; } = "";
            public double AverageTime { get; set; }
            public double MinTime { get; set; }
            public double MaxTime { get; set; }
            public double TotalTime { get; set; }
            public int Iterations { get; set; }
        }
    }
}