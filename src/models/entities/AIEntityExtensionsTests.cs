using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProceduralMiniGameGenerator.Models;

namespace ProceduralMiniGameGenerator.Models.Entities.Tests
{
    [TestClass]
    public class AIEntityExtensionsTests
    {
        private EnemyEntity _testEntity;

        [TestInitialize]
        public void Setup()
        {
            _testEntity = new EnemyEntity();
        }

        [TestMethod]
        public void GetAIDescription_WithNoAIContent_ReturnsNull()
        {
            // Act
            var result = _testEntity.GetAIDescription();

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public void GetAIDescription_WithAIDescription_ReturnsDescription()
        {
            // Arrange
            var expectedDescription = "Test AI description";
            _testEntity.SetAIDescription(expectedDescription);

            // Act
            var result = _testEntity.GetAIDescription();

            // Assert
            Assert.AreEqual(expectedDescription, result);
        }

        [TestMethod]
        public void GetAIDialogue_WithNoDialogue_ReturnsNull()
        {
            // Act
            var result = _testEntity.GetAIDialogue();

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public void GetAIDialogue_WithAIDialogue_ReturnsDialogue()
        {
            // Arrange
            var expectedDialogue = new[] { "Line 1", "Line 2", "Line 3" };
            _testEntity.SetAIDialogue(expectedDialogue);

            // Act
            var result = _testEntity.GetAIDialogue();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedDialogue.Length, result.Length);
            CollectionAssert.AreEqual(expectedDialogue, result);
        }

        [TestMethod]
        public void GetRandomDialogueLine_WithNoDialogue_ReturnsNull()
        {
            // Act
            var result = _testEntity.GetRandomDialogueLine();

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public void GetRandomDialogueLine_WithDialogue_ReturnsValidLine()
        {
            // Arrange
            var dialogue = new[] { "Line 1", "Line 2", "Line 3" };
            _testEntity.SetAIDialogue(dialogue);

            // Act
            var result = _testEntity.GetRandomDialogueLine();

            // Assert
            Assert.IsNotNull(result);
            CollectionAssert.Contains(dialogue, result);
        }

        [TestMethod]
        public void HasAIContent_WithNoContent_ReturnsFalse()
        {
            // Act
            var result = _testEntity.HasAIContent();

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void HasAIContent_WithDescription_ReturnsTrue()
        {
            // Arrange
            _testEntity.SetAIDescription("Test description");

            // Act
            var result = _testEntity.HasAIContent();

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void HasAIContent_WithDialogue_ReturnsTrue()
        {
            // Arrange
            _testEntity.SetAIDialogue(new[] { "Test dialogue" });

            // Act
            var result = _testEntity.HasAIContent();

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void GetDialogueLineCount_WithNoDialogue_ReturnsZero()
        {
            // Act
            var result = _testEntity.GetDialogueLineCount();

            // Assert
            Assert.AreEqual(0, result);
        }

        [TestMethod]
        public void GetDialogueLineCount_WithDialogue_ReturnsCorrectCount()
        {
            // Arrange
            var dialogue = new[] { "Line 1", "Line 2", "Line 3" };
            _testEntity.SetAIDialogue(dialogue);

            // Act
            var result = _testEntity.GetDialogueLineCount();

            // Assert
            Assert.AreEqual(3, result);
        }

        [TestMethod]
        public void SetAIDescription_WithValidDescription_SetsProperties()
        {
            // Arrange
            var description = "Test AI description";

            // Act
            _testEntity.SetAIDescription(description);

            // Assert
            Assert.AreEqual(description, _testEntity.Properties["Description"]);
            Assert.AreEqual(true, _testEntity.Properties["AIGenerated"]);
        }

        [TestMethod]
        public void SetAIDescription_WithEmptyDescription_DoesNotSetProperties()
        {
            // Act
            _testEntity.SetAIDescription("");

            // Assert
            Assert.IsFalse(_testEntity.Properties.ContainsKey("Description"));
            Assert.IsFalse(_testEntity.Properties.ContainsKey("AIGenerated"));
        }

        [TestMethod]
        public void SetAIDescription_WithNullDescription_DoesNotSetProperties()
        {
            // Act
            _testEntity.SetAIDescription(null);

            // Assert
            Assert.IsFalse(_testEntity.Properties.ContainsKey("Description"));
            Assert.IsFalse(_testEntity.Properties.ContainsKey("AIGenerated"));
        }

        [TestMethod]
        public void SetAIDialogue_WithValidDialogue_SetsProperties()
        {
            // Arrange
            var dialogue = new[] { "Line 1", "Line 2" };

            // Act
            _testEntity.SetAIDialogue(dialogue);

            // Assert
            Assert.AreEqual(dialogue, _testEntity.Properties["Dialogue"]);
            Assert.AreEqual(2, _testEntity.Properties["DialogueCount"]);
            Assert.AreEqual(true, _testEntity.Properties["AIGeneratedDialogue"]);
        }

        [TestMethod]
        public void SetAIDialogue_WithEmptyArray_DoesNotSetProperties()
        {
            // Act
            _testEntity.SetAIDialogue(new string[0]);

            // Assert
            Assert.IsFalse(_testEntity.Properties.ContainsKey("Dialogue"));
            Assert.IsFalse(_testEntity.Properties.ContainsKey("DialogueCount"));
            Assert.IsFalse(_testEntity.Properties.ContainsKey("AIGeneratedDialogue"));
        }

        [TestMethod]
        public void SetAIDialogue_WithNullArray_DoesNotSetProperties()
        {
            // Act
            _testEntity.SetAIDialogue(null);

            // Assert
            Assert.IsFalse(_testEntity.Properties.ContainsKey("Dialogue"));
            Assert.IsFalse(_testEntity.Properties.ContainsKey("DialogueCount"));
            Assert.IsFalse(_testEntity.Properties.ContainsKey("AIGeneratedDialogue"));
        }

        [TestMethod]
        public void GetAIContentSummary_WithNoContent_ReturnsEmptySummary()
        {
            // Act
            var summary = _testEntity.GetAIContentSummary();

            // Assert
            Assert.IsFalse(summary.HasDescription);
            Assert.IsFalse(summary.HasDialogue);
            Assert.AreEqual(0, summary.DialogueLineCount);
            Assert.AreEqual(EntityType.Enemy, summary.EntityType);
            Assert.IsFalse(summary.HasAnyContent);
        }

        [TestMethod]
        public void GetAIContentSummary_WithContent_ReturnsCorrectSummary()
        {
            // Arrange
            _testEntity.SetAIDescription("Test description");
            _testEntity.SetAIDialogue(new[] { "Line 1", "Line 2" });

            // Act
            var summary = _testEntity.GetAIContentSummary();

            // Assert
            Assert.IsTrue(summary.HasDescription);
            Assert.IsTrue(summary.HasDialogue);
            Assert.AreEqual(2, summary.DialogueLineCount);
            Assert.AreEqual(EntityType.Enemy, summary.EntityType);
            Assert.IsTrue(summary.HasAnyContent);
        }

        [TestMethod]
        public void GetRandomDialogueLine_MultipleCallsWithSingleLine_ReturnsSameLine()
        {
            // Arrange
            var dialogue = new[] { "Only line" };
            _testEntity.SetAIDialogue(dialogue);

            // Act
            var result1 = _testEntity.GetRandomDialogueLine();
            var result2 = _testEntity.GetRandomDialogueLine();

            // Assert
            Assert.AreEqual("Only line", result1);
            Assert.AreEqual("Only line", result2);
        }

        [TestMethod]
        public void GetDialogueLineCount_WithManuallySetCount_ReturnsManualCount()
        {
            // Arrange
            _testEntity.Properties["DialogueCount"] = 5;

            // Act
            var result = _testEntity.GetDialogueLineCount();

            // Assert
            Assert.AreEqual(5, result);
        }

        [TestMethod]
        public void GetDialogueLineCount_WithStringCount_ParsesCorrectly()
        {
            // Arrange
            _testEntity.Properties["DialogueCount"] = "7";

            // Act
            var result = _testEntity.GetDialogueLineCount();

            // Assert
            Assert.AreEqual(7, result);
        }

        [TestMethod]
        public void GetDialogueLineCount_WithInvalidStringCount_ReturnsZero()
        {
            // Arrange
            _testEntity.Properties["DialogueCount"] = "invalid";

            // Act
            var result = _testEntity.GetDialogueLineCount();

            // Assert
            Assert.AreEqual(0, result);
        }
    }
}