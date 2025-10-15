// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Test;

public class AnnotationExtensionsTests
{
    [Fact]
    public void WithAdditionalAnnotation_SingleAnnotation_AddsAnnotation()
    {
        // Arrange
        var node = CreateTestNode();
        var annotation = new SyntaxAnnotation("test");

        // Act
        var newNode = node.WithAdditionalAnnotation(annotation);

        // Assert
        Assert.True(newNode.HasAnnotation(annotation));
        Assert.False(node.HasAnnotation(annotation)); // Original unchanged
        Assert.NotSame(node, newNode);
    }

    [Fact]
    public void WithAdditionalAnnotation_NullAnnotation_ThrowsArgumentNullException()
    {
        // Arrange
        var node = CreateTestNode();

        // Act & Assert
        Assert.Throws<System.ArgumentNullException>(() => node.WithAdditionalAnnotation(null!));
    }

    [Fact]
    public void WithAdditionalAnnotations_Enumerable_AddsAllAnnotations1()
    {
        // Arrange
        var node = CreateTestNode();
        var annotation1 = new SyntaxAnnotation("test1");
        var annotation2 = new SyntaxAnnotation("test2");

        // Act
        var newNode = node.WithAdditionalAnnotations(annotation1, annotation2);

        // Assert
        Assert.True(newNode.HasAnnotation(annotation1));
        Assert.True(newNode.HasAnnotation(annotation2));
        Assert.False(node.HasAnnotation(annotation1));
        Assert.False(node.HasAnnotation(annotation2));
        Assert.NotSame(node, newNode);
    }

    [Fact]
    public void WithAdditionalAnnotations_Enumerable_AddsAllAnnotations2()
    {
        // Arrange
        var node = CreateTestNode();
        var annotations = new List<SyntaxAnnotation>
        {
            new("test1"),
            new("test2"),
            new("test3")
        };

        // Act
        var newNode = node.WithAdditionalAnnotations(annotations);

        // Assert
        foreach (var annotation in annotations)
        {
            Assert.True(newNode.HasAnnotation(annotation));
            Assert.False(node.HasAnnotation(annotation));
        }
        Assert.NotSame(node, newNode);
    }

    [Fact]
    public void WithAdditionalAnnotations_EmptyEnumerable_ReturnsOriginalNode()
    {
        // Arrange
        var node = CreateTestNode();

        // Act
        var newNode = node.WithAdditionalAnnotations();

        // Assert
        Assert.Same(node, newNode);
    }

    [Fact]
    public void WithAdditionalAnnotations_NullEnumerable_ThrowsArgumentNullException()
    {
        // Arrange
        var node = CreateTestNode();

        // Act & Assert
        Assert.Throws<System.ArgumentNullException>(() => node.WithAdditionalAnnotations(null!));
    }

    [Fact]
    public void WithoutAnnotation_ExistingAnnotation_RemovesAnnotation()
    {
        // Arrange
        var annotation = new SyntaxAnnotation("test");
        var node = CreateTestNode().WithAdditionalAnnotation(annotation);

        // Act
        var newNode = node.WithoutAnnotation(annotation);

        // Assert
        Assert.False(newNode.HasAnnotation(annotation));
        Assert.True(node.HasAnnotation(annotation)); // Original unchanged
        Assert.NotSame(node, newNode);
    }

    [Fact]
    public void WithoutAnnotation_NonExistentAnnotation_ReturnsOriginalNode()
    {
        // Arrange
        var node = CreateTestNode();
        var annotation = new SyntaxAnnotation("test");

        // Act
        var newNode = node.WithoutAnnotation(annotation);

        // Assert
        Assert.Same(node, newNode);
    }

    [Fact]
    public void WithoutAnnotation_NullAnnotation_ThrowsArgumentNullException()
    {
        // Arrange
        var node = CreateTestNode();

        // Act & Assert
        Assert.Throws<System.ArgumentNullException>(() => node.WithoutAnnotation(null!));
    }

    [Fact]
    public void WithoutAnnotations_AnnotationKind_RemovesAllAnnotationsOfKind()
    {
        // Arrange
        var annotation1 = new SyntaxAnnotation("test", "data1");
        var annotation2 = new SyntaxAnnotation("test", "data2");
        var annotation3 = new SyntaxAnnotation("other");
        var node = CreateTestNode()
            .WithAdditionalAnnotation(annotation1)
            .WithAdditionalAnnotation(annotation2)
            .WithAdditionalAnnotation(annotation3);

        // Act
        var newNode = node.WithoutAnnotations("test");

        // Assert
        Assert.False(newNode.HasAnnotation(annotation1));
        Assert.False(newNode.HasAnnotation(annotation2));
        Assert.True(newNode.HasAnnotation(annotation3));
        Assert.True(node.HasAnnotation(annotation1));
        Assert.True(node.HasAnnotation(annotation2));
        Assert.NotSame(node, newNode);
    }

    [Fact]
    public void WithoutAnnotations_NonExistentAnnotationKind_ReturnsOriginalNode()
    {
        // Arrange
        var node = CreateTestNode();

        // Act
        var newNode = node.WithoutAnnotations("nonexistent");

        // Assert
        Assert.Same(node, newNode);
    }

    [Fact]
    public void WithoutAnnotations_NullAnnotationKind_ThrowsArgumentNullException()
    {
        // Arrange
        var node = CreateTestNode();

        // Act & Assert
        Assert.Throws<System.ArgumentNullException>(() => node.WithoutAnnotations((string)null!));
    }

    [Fact]
    public void WithoutAnnotations_Enumerable_RemovesSpecifiedAnnotations()
    {
        // Arrange
        var annotation1 = new SyntaxAnnotation("test1");
        var annotation2 = new SyntaxAnnotation("test2");
        var annotation3 = new SyntaxAnnotation("test3");
        var node = CreateTestNode()
            .WithAdditionalAnnotation(annotation1)
            .WithAdditionalAnnotation(annotation2)
            .WithAdditionalAnnotation(annotation3);

        // Act
        var newNode = node.WithoutAnnotations(annotation1, annotation3);

        // Assert
        Assert.False(newNode.HasAnnotation(annotation1));
        Assert.True(newNode.HasAnnotation(annotation2));
        Assert.False(newNode.HasAnnotation(annotation3));
        Assert.True(node.HasAnnotation(annotation1));
        Assert.True(node.HasAnnotation(annotation3));
        Assert.NotSame(node, newNode);
    }

    [Fact]
    public void WithoutAnnotations_EmptyEnumerable_ReturnsOriginalNode()
    {
        // Arrange
        var node = CreateTestNode();

        // Act
        var newNode = node.WithoutAnnotations();

        // Assert
        Assert.Same(node, newNode);
    }

    [Fact]
    public void WithoutAnnotations_NullEnumerable_ThrowsArgumentNullException()
    {
        // Arrange
        var node = CreateTestNode();

        // Act & Assert
        Assert.Throws<System.ArgumentNullException>(() => node.WithoutAnnotations((IEnumerable<SyntaxAnnotation>)null!));
    }

    [Fact]
    public void ChainedOperations_AddAndRemove_WorksCorrectly()
    {
        // Arrange
        var node = CreateTestNode();
        var annotation1 = new SyntaxAnnotation("test1");
        var annotation2 = new SyntaxAnnotation("test2");
        var annotation3 = new SyntaxAnnotation("test3");

        // Act
        var newNode = node
            .WithAdditionalAnnotation(annotation1)
            .WithAdditionalAnnotations(annotation2, annotation3)
            .WithoutAnnotation(annotation2);

        // Assert
        Assert.True(newNode.HasAnnotation(annotation1));
        Assert.False(newNode.HasAnnotation(annotation2));
        Assert.True(newNode.HasAnnotation(annotation3));
        Assert.False(node.HasAnnotation(annotation1)); // Original unchanged
    }

    [Fact]
    public void WithAdditionalAnnotations_DuplicateAnnotations_DoesNotAddDuplicates()
    {
        // Arrange
        var node = CreateTestNode();
        var annotation = new SyntaxAnnotation("test");

        // Act
        var newNode = node
            .WithAdditionalAnnotation(annotation)
            .WithAdditionalAnnotation(annotation);

        // Assert
        Assert.True(newNode.HasAnnotation(annotation));
        // Should only have one instance of the annotation
        Assert.Single(newNode.GetAnnotations("test"));
    }

    [Fact]
    public void WithAdditionalAnnotations_PreservesExistingAnnotations()
    {
        // Arrange
        var existingAnnotation = new SyntaxAnnotation("existing");
        var node = CreateTestNode().WithAdditionalAnnotation(existingAnnotation);
        var newAnnotation = new SyntaxAnnotation("new");

        // Act
        var newNode = node.WithAdditionalAnnotation(newAnnotation);

        // Assert
        Assert.True(newNode.HasAnnotation(existingAnnotation));
        Assert.True(newNode.HasAnnotation(newAnnotation));
    }

    private static SyntaxNode CreateTestNode()
    {
        return SyntaxFactory.RazorDocument(
            SyntaxFactory.GenericBlock());
    }
}
