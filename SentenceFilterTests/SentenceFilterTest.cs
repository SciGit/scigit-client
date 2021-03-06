﻿using SciGit_Filter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace SentenceFilterTests
{
    
    
    /// <summary>
    ///This is a test class for SentenceFilterTest and is intended
    ///to contain all SentenceFilterTest Unit Tests
    ///</summary>
  [TestClass()]
  public class SentenceFilterTest
  {


    private TestContext testContextInstance;

    /// <summary>
    ///Gets or sets the test context which provides
    ///information about and functionality for the current test run.
    ///</summary>
    public TestContext TestContext {
      get {
        return testContextInstance;
      }
      set {
        testContextInstance = value;
      }
    }

    #region Additional test attributes
    // 
    //You can use the following additional attributes as you write your tests:
    //
    //Use ClassInitialize to run code before running the first test in the class
    //[ClassInitialize()]
    //public static void MyClassInitialize(TestContext testContext)
    //{
    //}
    //
    //Use ClassCleanup to run code after all tests in a class have run
    //[ClassCleanup()]
    //public static void MyClassCleanup()
    //{
    //}
    //
    //Use TestInitialize to run code before running each test
    //[TestInitialize()]
    //public void MyTestInitialize()
    //{
    //}
    //
    //Use TestCleanup to run code after each test has run
    //[TestCleanup()]
    //public void MyTestCleanup()
    //{
    //}
    //
    #endregion

    public void RunCleanTest(string str, string expected) {
      Assert.AreEqual(expected, SentenceFilter.Clean(str));
      Assert.AreEqual(str, SentenceFilter.Smudge(SentenceFilter.Clean(str)));
    }

    public void RunSmudgeTest(string str, string expected) {
      Assert.AreEqual(expected, SentenceFilter.Smudge(str));
    }

    [TestMethod()]
    public void CleanEmptyTest() {
      RunCleanTest("", "");
    }

    [TestMethod()]
    public void CleanSplitSentenceTest() {
      SentenceFilter.MergeSentences = true;
      RunCleanTest("I like\n\tturtles.", String.Format("I like{0}\tturtles.", SentenceFilter.MergedNewlineDelim));
      RunCleanTest("I like\r\n\tturtles.", String.Format("I like{0}\tturtles.", SentenceFilter.MergedWindowsNewlineDelim));
      RunCleanTest("I \\begin{blah}\nlike turtles.\n\\end{blah}", "I \\begin{blah}\nlike turtles.\n\\end{blah}");
    }

    [TestMethod()]
    public void CleanMultipleSentenceTest() {
      RunCleanTest("I like turtles. I also like pie. Said A. Lincoln",
        "I like turtles. #\nI also like pie. #\nSaid A. Lincoln".Replace("#", SentenceFilter.SentenceDelim));
    }

    [TestMethod()]
    public void SmudgeEmptyTest() {
      RunSmudgeTest("", "");
    }
  }
}
