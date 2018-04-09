﻿using System;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using NUnit.Framework;

namespace RoslynNunitTestRunner
{
    public static class Verify
    {
        public static void CodeAction(CodeAction codeAction, Document document, string expectedCode)
        {
            var operations = codeAction.GetOperationsAsync(CancellationToken.None).Result;

            Assert.That(operations.Count(), Is.EqualTo(1));

            var operation = operations.Single();
            var workspace = document.Project.Solution.Workspace;
            operation.Apply(workspace, CancellationToken.None);

            var newDocument = workspace.CurrentSolution.GetDocument(document.Id);

            var sourceText = newDocument.GetTextAsync(CancellationToken.None).Result;
            var text = sourceText.ToString();
            Console.WriteLine($"New code:\r\n{text}");

            // Need to replace win-style line ending to unix-style to avoid build breaks on AppVeyor
            text = text.Replace("\r\n", "\n");
            expectedCode = expectedCode.Replace("\r\n", "\n");

            Assert.That(text, Is.EqualTo(expectedCode));
        }
    }
}
