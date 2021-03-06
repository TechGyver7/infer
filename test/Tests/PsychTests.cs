// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Xunit;
using Microsoft.ML.Probabilistic.Utilities;
using Microsoft.ML.Probabilistic.Distributions;
using Microsoft.ML.Probabilistic.Models;
using Microsoft.ML.Probabilistic.Math;
using System.IO;
using Microsoft.ML.Probabilistic.Algorithms;
using Microsoft.ML.Probabilistic.Serialization;

namespace Microsoft.ML.Probabilistic.Tests
{
    using Assert = Xunit.Assert;

    
    public class PsychTests
    {
#if SUPPRESS_UNREACHABLE_CODE_WARNINGS
#pragma warning disable 162
#endif

        internal void LogisticIrtTest()
        {
            Variable<int> numStudents = Variable.New<int>().Named("numStudents");
            Range student = new Range(numStudents);
            VariableArray<double> ability = Variable.Array<double>(student).Named("ability");
            ability[student] = Variable.GaussianFromMeanAndPrecision(0, 1e-6).ForEach(student);
            Variable<int> numQuestions = Variable.New<int>().Named("numQuestions");
            Range question = new Range(numQuestions);
            VariableArray<double> difficulty = Variable.Array<double>(question).Named("difficulty");
            difficulty[question] = Variable.GaussianFromMeanAndPrecision(0, 1e-6).ForEach(question);
            VariableArray<double> discrimination = Variable.Array<double>(question).Named("discrimination");
            discrimination[question] = Variable.Exp(Variable.GaussianFromMeanAndPrecision(0, 1).ForEach(question));
            VariableArray2D<bool> response = Variable.Array<bool>(student, question).Named("response");
            response[student, question] = Variable.BernoulliFromLogOdds(((ability[student] - difficulty[question]).Named("minus")*discrimination[question]).Named("product"));
            bool[,] data;
            double[] discriminationTrue = new double[0];
            if (false)
            {
                data = new bool[4,2];
                for (int i = 0; i < data.GetLength(0); i++)
                {
                    for (int j = 0; j < data.GetLength(1); j++)
                    {
                        data[i, j] = (i > j);
                    }
                }
            }
            else
            {
                // simulated data
                // also try IRT2PL_10_250.mat
                //TODO: change path for cross platform using
                Dictionary<string, object> dict = MatlabReader.Read(@"..\..\..\Tests\Data\IRT2PL_10_1000.mat");
                Matrix m = (Matrix) dict["Y"];
                data = ConvertToBool(m.ToArray());
                m = (Matrix) dict["discrimination"];
                discriminationTrue = Util.ArrayInit(data.GetLength(1), i => m[i]);
            }
            numStudents.ObservedValue = data.GetLength(0);
            numQuestions.ObservedValue = data.GetLength(1);
            response.ObservedValue = data;
            InferenceEngine engine = new InferenceEngine();
            engine.Algorithm = new VariationalMessagePassing();
            Console.WriteLine(StringUtil.JoinColumns(engine.Infer(discrimination), " should be ", StringUtil.ToString(discriminationTrue)));
        }

#if SUPPRESS_UNREACHABLE_CODE_WARNINGS
#pragma warning restore 162
#endif

        public static bool[,] ConvertToBool(double[,] array)
        {
            int rows = array.GetLength(0);
            int cols = array.GetLength(1);
            bool[,] result = new bool[rows,cols];
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    result[i, j] = (array[i, j] > 0);
                }
            }
            return result;
        }

        [Fact]
        //[DeploymentItem(@"Data\IRT2PL_10_250.mat", "Data")]
        public void MatlabReaderTest2()
        {
            Dictionary<string, object> dict = MatlabReader.Read(Path.Combine(
#if NETCORE
                Path.GetDirectoryName(typeof(PsychTests).Assembly.Location), // work dir is not the one with Microsoft.ML.Probabilistic.Tests.dll on netcore and neither is .Location on netfull
#endif
                "Data", "IRT2PL_10_250.mat"));
            Assert.Equal(5, dict.Count);
            Matrix m = (Matrix) dict["Y"];
            Assert.True(m.Rows == 250);
            Assert.True(m.Cols == 10);
            Assert.True(m[0, 1] == 0.0);
            Assert.True(m[1, 0] == 1.0);
            m = (Matrix) dict["difficulty"];
            Assert.True(m.Rows == 10);
            Assert.True(m.Cols == 1);
            Assert.True(MMath.AbsDiff(m[1], 0.7773) < 2e-4);
        }

        [Fact]
        ////[DeploymentItem(@"Data\test.mat", "Data")]
        public void MatlabReaderTest()
        {
            MatlabReaderTester(Path.Combine(
#if NETCORE
                Path.GetDirectoryName(typeof(PsychTests).Assembly.Location), // work dir is not the one with Microsoft.ML.Probabilistic.Tests.dll on netcore and neither is .Location on netfull
#endif
                "Data", "test.mat"));
        }

        private void MatlabReaderTester(string fileName)
        {
            Dictionary<string, object> dict = MatlabReader.Read(fileName);
            Assert.Equal(12, dict.Count);
            Matrix aScalar = (Matrix) dict["aScalar"];
            Assert.Equal(1, aScalar.Rows);
            Assert.Equal(1, aScalar.Cols);
            Assert.Equal(5.0, aScalar[0, 0]);
            Assert.Equal("string", (string) dict["aString"]);
            MatlabReader.ComplexMatrix aComplexScalar = (MatlabReader.ComplexMatrix) dict["aComplexScalar"];
            Assert.Equal(5.0, aComplexScalar.Real[0, 0]);
            Assert.Equal(3.0, aComplexScalar.Imaginary[0, 0]);
            MatlabReader.ComplexMatrix aComplexVector = (MatlabReader.ComplexMatrix) dict["aComplexVector"];
            Assert.Equal(1.0, aComplexVector.Real[0, 0]);
            Assert.Equal(2.0, aComplexVector.Imaginary[0, 0]);
            Assert.Equal(3.0, aComplexVector.Real[0, 1]);
            Assert.Equal(4.0, aComplexVector.Imaginary[0, 1]);
            var aStruct = (Dictionary<string, object>) dict["aStruct"];
            Assert.Equal(2, aStruct.Count);
            Assert.Equal(1.0, ((Matrix) aStruct["field1"])[0]);
            Assert.Equal("two", (string) aStruct["field2"]);
            object[,] aCell = (object[,]) dict["aCell"];
            Assert.Equal(1.0, ((Matrix) aCell[0, 0])[0]);
            int[] intArray = (int[]) dict["intArray"];
            Assert.Equal(1, intArray[0]);
            int[] uintArray = (int[])dict["uintArray"];
            Assert.Equal(1, uintArray[0]);
            bool[] aLogical = (bool[]) dict["aLogical"];
            Assert.True(aLogical[0]);
            Assert.True(aLogical[1]);
            Assert.False(aLogical[2]);
            object[,,] aCell3D = (object[,,]) dict["aCell3D"];
            Assert.Null(aCell3D[0, 0, 0]);
            Assert.Equal(7.0, ((Matrix) aCell3D[0, 0, 1])[0, 0]);
            Assert.Equal(6.0, ((Matrix) aCell3D[0, 1, 0])[0, 0]);
            double[,,,] array4D = (double[,,,]) dict["array4D"];
            Assert.Equal(4.0, array4D[0, 0, 1, 0]);
            Assert.Equal(5.0, array4D[0, 0, 0, 1]);
            long[] aLong = (long[]) dict["aLong"];
            Assert.Equal(1234567890123456789L, aLong[0]);
        }

        [Fact]
        //[DeploymentItem(@"Data\test.mat", "Data")]
        public void MatlabWriterTest()
        {
            Dictionary<string, object> dict = MatlabReader.Read(Path.Combine(
#if NETCORE
                Path.GetDirectoryName(typeof(PsychTests).Assembly.Location), // work dir is not the one with Microsoft.ML.Probabilistic.Tests.dll on netcore and neither is .Location on netfull
#endif
                "Data", "test.mat"));
            string fileName = $"{System.IO.Path.GetTempPath()}MatlabWriterTest{Environment.CurrentManagedThreadId}.mat";
            using (MatlabWriter writer = new MatlabWriter(fileName))
            {
                foreach (var entry in dict)
                {
                    writer.Write(entry.Key, entry.Value);
                }
            }
            MatlabReaderTester(fileName);
        }

        [Fact]
        public void MatlabWriteStringDictionaryTest()
        {
            Dictionary<string, string> dictString = new Dictionary<string, string>();
            dictString["a"] = "a";
            dictString["b"] = "b";
            string fileName = $"{System.IO.Path.GetTempPath()}MatlabWriteStringDictionaryTest{Environment.CurrentManagedThreadId}.mat";
            using (MatlabWriter writer = new MatlabWriter(fileName))
            {
                writer.Write("dictString", dictString);
            }
            Dictionary<string, object> vars = MatlabReader.Read(fileName);
            Dictionary<string, object> dict = (Dictionary<string, object>)vars["dictString"];
            foreach (var entry in dictString)
            {
                Assert.Equal(dictString[entry.Key], dict[entry.Key]);
            }
        }

        [Fact]
        public void MatlabWriteStringListTest()
        {
            List<string> strings = new List<string>();
            strings.Add("a");
            strings.Add("b");
            string fileName = $"{System.IO.Path.GetTempPath()}MatlabWriteStringListTest{Environment.CurrentManagedThreadId}.mat";
            using (MatlabWriter writer = new MatlabWriter(fileName))
            {
                writer.Write("strings", strings);
            }
            Dictionary<string, object> vars = MatlabReader.Read(fileName);
            string[] array = (string[])vars["strings"];
            for (int i = 0; i < array.Length; i++)
            {
                Assert.Equal(strings[i], array[i]);
            }
        }

        [Fact]
        public void MatlabWriteEmptyArrayTest()
        {
            string fileName = $"{System.IO.Path.GetTempPath()}MatlabWriteEmptyArrayTest{Environment.CurrentManagedThreadId}.mat";
            using (MatlabWriter writer = new MatlabWriter(fileName))
            {
                writer.Write("ints", new int[0]);
            }
            Dictionary<string, object> vars = MatlabReader.Read(fileName);
            int[] ints = (int[])vars["ints"];
            Assert.Empty(ints);
        }

        [Fact]
        public void MatlabWriteNumericNameTest()
        {
            string fileName = $"{System.IO.Path.GetTempPath()}MatlabWriteNumericNameTest{Environment.CurrentManagedThreadId}.mat";
            using (MatlabWriter writer = new MatlabWriter(fileName))
            {
                writer.Write("24", new int[0]);
            }
            Dictionary<string, object> vars = MatlabReader.Read(fileName);
            int[] ints = (int[])vars["24"];
            Assert.Empty(ints);
        }

#if SUPPRESS_UNREACHABLE_CODE_WARNINGS
#pragma warning disable 162
#endif

        /// <summary>
        /// Nonconjugate VMP crashes with improper message on the first iteration.
        /// </summary>
        internal void LogisticIrtTestWithTruncatedGaussian()
        {
            Variable<int> numStudents = Variable.New<int>().Named("numStudents");
            Range student = new Range(numStudents);
            VariableArray<double> ability = Variable.Array<double>(student).Named("ability");
            ability[student] = Variable.GaussianFromMeanAndPrecision(0, 1e-2).ForEach(student);
            Variable<int> numQuestions = Variable.New<int>().Named("numQuestions");
            Range question = new Range(numQuestions);
            VariableArray<double> difficulty = Variable.Array<double>(question).Named("difficulty");
            difficulty[question] = Variable.GaussianFromMeanAndPrecision(0, 1e-2).ForEach(question);
            var response = Variable.Array<bool>(student, question).Named("response");
            var minus = Variable.Array<double>(student, question).Named("minus");
            //var discrimination = Variable.Array<double>(question).Named("discrimination");
            //discrimination[question] = Variable.TruncatedGaussian(1, 1/1.5e-3, 0, double.PositiveInfinity).ForEach(question);
            var disc2 = Variable.Array<double>(question).Named("disc2");
            //disc2[question] = Variable.Copy(discrimination[question]);
            //disc2.AddAttribute(new MarginalPrototype(new Gaussian())); 
            disc2[question] = Variable.GaussianFromMeanAndVariance(1, 1/1.5e-3).ForEach(question);
            Variable.ConstrainPositive(disc2[question]);
            minus[student, question] = (ability[student] - difficulty[question]);
            var product = Variable.Array<double>(student, question).Named("product");
            product[student, question] = minus[student, question]*disc2[question];
            response[student, question] = Variable.BernoulliFromLogOdds(product[student, question]);
            //response.AddAttribute(new MarginalPrototype(new Gaussian())); 
            bool[,] data;
            double[] discriminationTrue = new double[0];
            if (false)
            {
                data = new bool[4,2];
                for (int i = 0; i < data.GetLength(0); i++)
                {
                    for (int j = 0; j < data.GetLength(1); j++)
                    {
                        data[i, j] = (i > j);
                    }
                }
            }
            else
            {
                // simulated data
                // also try IRT2PL_10_250.mat
                //TODO: change path for cross platform using
                Dictionary<string, object> dict = MatlabReader.Read(@"..\..\..\Tests\Data\IRT2PL_10_1000.mat");
                Matrix m = (Matrix) dict["Y"];
                data = ConvertToBool(m.ToArray());
                m = (Matrix) dict["discrimination"];
                discriminationTrue = Util.ArrayInit(data.GetLength(1), i => m[i]);
            }
            numStudents.ObservedValue = data.GetLength(0);
            numQuestions.ObservedValue = data.GetLength(1);
            response.ObservedValue = data;
            InferenceEngine engine = new InferenceEngine();
            engine.Algorithm = new VariationalMessagePassing();
            engine.ShowTimings = true;
            Console.WriteLine("Compare inferred logDiscrimination to ground truth:");
            //var marg = engine.Infer<DistributionArray<TruncatedGaussian>>(discrimination);
            var marg = engine.Infer<DistributionArray<Gaussian>>(disc2);
            for (int i = 0; i < data.GetLength(1); i++)
                Console.WriteLine(marg[i].GetMean() + " \t " + discriminationTrue[i]);
        }

#if SUPPRESS_UNREACHABLE_CODE_WARNINGS
#pragma warning restore 162
#endif

        internal void LogisticIrtProductExpTest()
        {
            int numStudents = 20;
            Range student = new Range(numStudents).Named("students");
            var ability = Variable.Array<double>(student).Named("ability");
            ability[student] = Variable.GaussianFromMeanAndPrecision(0, 1e-6).ForEach(student);
            int numQuestions = 4;
            Range question = new Range(numQuestions).Named("questions");
            var difficulty = Variable.Array<double>(question).Named("difficulty");
            difficulty[question] = Variable.GaussianFromMeanAndPrecision(0, 1e-6).ForEach(question);
            var logDisc = Variable.Array<double>(question).Named("logDisc");
            logDisc[question] = Variable.GaussianFromMeanAndPrecision(0, 1).ForEach(question);
            var response = Variable.Array<bool>(student, question).Named("response");
            var minus = Variable.Array<double>(student, question).Named("minus");
            minus[student, question] = (ability[student] - difficulty[question]);
            var product = Variable.Array<double>(student, question).Named("product");
            product[student, question] = Variable.ProductExp(minus[student, question], logDisc[question]);
            response[student, question] = Variable.BernoulliFromLogOdds(product[student, question]);
            bool[,] data = new bool[numStudents,numQuestions];
            for (int i = 0; i < numStudents; i++)
            {
                for (int j = 0; j < numQuestions; j++)
                {
                    data[i, j] = (i > j);
                }
            }
            response.ObservedValue = data;
            InferenceEngine engine = new InferenceEngine();
            engine.ShowFactorGraph = true;
            engine.Algorithm = new VariationalMessagePassing();
            Console.WriteLine(engine.Infer(logDisc));
        }
    }
}