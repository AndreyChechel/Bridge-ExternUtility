using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BridgeExternUtility
{
    public static class RoslynHelper
    {
        private static readonly HashSet<string> EmptyResultSet = new HashSet<string> { "0", "false", "null", "default(T)" };

        /// <summary>
        /// Transforms an Empty method to an Extern method without body;
        /// </summary>
        public static bool TransformEmptyMethodToExtern(ref BaseMethodDeclarationSyntax method)
        {
            if (IsModifierApplied(method, SyntaxKind.ExternKeyword) ||
                IsModifierApplied(method, SyntaxKind.AbstractKeyword) ||
                IsModifierApplied(method, SyntaxKind.VirtualKeyword) ||
                !IsMethodWithEmptyBody(method))
            {
                return false;
            }

            var updatedMethod = AddMethodModifier(method, SyntaxKind.ExternKeyword);
            updatedMethod = RemoveMethodBody(updatedMethod, true);

            method = updatedMethod;
            return true;
        }

        /// <summary>
        /// Checks if a method declaration contains the specified modifier.
        /// </summary>
        public static bool IsModifierApplied(BaseMethodDeclarationSyntax method, SyntaxKind modifierKind)
        {
            return method.Modifiers.Any(x => x.Kind() == modifierKind);
        }

        /// <summary>
        /// Checks if a method has empty body or a function has just a return statement.
        /// </summary>
        public static bool IsMethodWithEmptyBody(BaseMethodDeclarationSyntax method)
        {
            if (IsFunction(method))
            {
                if (method.Body.Statements.Count == 1)
                {
                    var retValue = GetReturningValue(method);
                    return EmptyResultSet.Contains(retValue);
                }
            }
            else
            {
                return method.Body.Statements.Count == 0;
            }

            return false;
        }

        /// <summary>
        /// Checks whether it is a method or function.
        /// </summary>
        public static bool IsFunction(BaseMethodDeclarationSyntax method)
        {
            var retTypeSyntax = method.GetReturnType() as PredefinedTypeSyntax;
            return retTypeSyntax == null || retTypeSyntax.Keyword.Kind() != SyntaxKind.VoidKeyword;
        }

        /// <summary>
        /// Gets returning value from the single "return {};" statement.
        /// </summary>
        private static string GetReturningValue(BaseMethodDeclarationSyntax method)
        {
            var statement = method.Body.Statements.FirstOrDefault(x => x.Kind() == SyntaxKind.ReturnStatement);

            var retStatement = statement as ReturnStatementSyntax;
            if (retStatement != null)
            {
                var singleNode = retStatement.ChildNodesAndTokens().SingleOrDefault(x => x.IsNode);
                if (singleNode != null)
                {
                    return singleNode.ToString();
                }
            }

            return null;
        }

        /// <summary>
        /// Removes the body of the specified method.
        /// </summary>
        public static BaseMethodDeclarationSyntax RemoveMethodBody(BaseMethodDeclarationSyntax method, bool addSemicolon)
        {
            var updatedMethod = method.ReplaceNode(method.Body, (SyntaxNode)null);

            if (addSemicolon)
            {
                var semicolonToken = SyntaxFactory.Token(SyntaxKind.SemicolonToken);
                UpdateForJoinFromRight(ref updatedMethod, ref semicolonToken);

                updatedMethod = updatedMethod.WithSemicolonToken(semicolonToken);
            }

            return updatedMethod;
        }

        /// <summary>
        /// Adds a modifier to the specified method.
        /// </summary>
        public static BaseMethodDeclarationSyntax AddMethodModifier(BaseMethodDeclarationSyntax method, SyntaxKind newModifierKind)
        {
            var updatedMethod = method;
            var newModifierToken = SyntaxFactory.Token(newModifierKind);

            var oldModifiers = method.Modifiers;
            var newModifiers = SyntaxFactory.TokenList(oldModifiers);

            if (newModifiers.Count > 0)
            {
                // If there is at least one modifier in the method declaration,
                // add new modifier at the end, separated by a whitespace

                var updatedLastModifier = newModifiers.Last();
                UpdateForJoinFromRight(ref updatedLastModifier, ref newModifierToken, SyntaxFactory.Space);

                newModifiers = newModifiers.RemoveAt(newModifiers.Count - 1);
                newModifiers = newModifiers.Add(updatedLastModifier);
            }
            else
            {
                // If there are no modifiers in the method declaration,
                // add new modifier before return type, separated by a whitespace

                var updatedRetType = updatedMethod.GetReturnType();
                UpdateForJoinFromLeft(ref updatedRetType, ref newModifierToken, SyntaxFactory.Space);

                updatedMethod = updatedMethod.ReplaceNode(updatedMethod.GetReturnType(), updatedRetType);
            }

            newModifiers = newModifiers.Add(newModifierToken);
            updatedMethod = updatedMethod.WithModifiers(newModifiers);

            return updatedMethod;
        }

        /// <summary>
        /// Prepares tokens for joining in the single node.
        /// {LeadingTrivia}{srcToken} -> {LeadingTrivia}{addingToken}{delimiterTrivias}{srcToken}
        /// </summary>
        private static void UpdateForJoinFromLeft(ref SyntaxToken srcToken, ref SyntaxToken addingToken, params SyntaxTrivia[] delimiterTrivias)
        {
            // If there are no modifiers in the method declaration,
            // add new modifier before return type, separated by a whitespace

            if (!srcToken.HasLeadingTrivia)
            {
                addingToken = addingToken.WithTrailingTrivia(SyntaxFactory.Space);
            }
            else
            {
                // Preserve the leading trivia of the return type statement,
                // attach this trivia to newModifier

                var srcTokenTrivia = srcToken.LeadingTrivia;
                srcToken = srcToken.ReplaceTrivia(srcTokenTrivia, (t1, t2) => default(SyntaxTrivia));
                srcToken = srcToken.WithLeadingTrivia(delimiterTrivias);

                addingToken = addingToken.WithLeadingTrivia(srcTokenTrivia);
            }
        }

        /// <summary>
        /// Prepares tokens for joining in the single node.
        /// {LeadingTrivia}{srcToken} -> {LeadingTrivia}{addingToken}{delimiterTrivias}{srcToken}
        /// </summary>
        private static void UpdateForJoinFromLeft<T>(ref T srcToken, ref SyntaxToken addingToken, params SyntaxTrivia[] delimiterTrivias)
            where T : SyntaxNode
        {
            // If there are no modifiers in the method declaration,
            // add new modifier before return type, separated by a whitespace

            if (!srcToken.HasLeadingTrivia)
            {
                addingToken = addingToken.WithTrailingTrivia(SyntaxFactory.Space);
            }
            else
            {
                // Preserve the leading trivia of the return type statement,
                // attach this trivia to newModifier

                var srcTokenTrivia = srcToken.GetLeadingTrivia();
                srcToken = srcToken.ReplaceTrivia(srcTokenTrivia, (t1, t2) => default(SyntaxTrivia));
                srcToken = srcToken.WithLeadingTrivia(delimiterTrivias);

                addingToken = addingToken.WithLeadingTrivia(srcTokenTrivia);
            }
        }

        /// <summary>
        /// Prepares tokens for joining in the single node.
        /// {srcToken}{TrailingTrivia} -> {srcToken}{delimiterTrivias}{addingToken}{TrailingTrivia}
        /// </summary>
        private static void UpdateForJoinFromRight(ref SyntaxToken srcToken, ref SyntaxToken addingToken, params SyntaxTrivia[] delimiterTrivias)
        {
            if (!srcToken.HasTrailingTrivia)
            {
                addingToken = addingToken.WithLeadingTrivia(SyntaxFactory.Space);
            }
            else
            {
                // Preserve the trailing trivia, attach this trivia to addingToken

                var srcTokenTrivia = srcToken.TrailingTrivia;
                srcToken = srcToken.ReplaceTrivia(srcTokenTrivia, (t1, t2) => default(SyntaxTrivia));
                srcToken = srcToken.WithTrailingTrivia(delimiterTrivias);

                addingToken = addingToken.WithTrailingTrivia(srcTokenTrivia);
            }
        }

        /// <summary>
        /// Prepares tokens for joining in the single node.
        /// {srcToken}{TrailingTrivia} -> {srcToken}{delimiterTrivias}{addingToken}{TrailingTrivia}
        /// </summary>
        private static void UpdateForJoinFromRight<T>(ref T srcToken, ref SyntaxToken addingToken, params SyntaxTrivia[] delimiterTrivias)
            where T : SyntaxNode
        {
            if (!srcToken.HasTrailingTrivia)
            {
                addingToken = addingToken.WithLeadingTrivia(SyntaxFactory.Space);
            }
            else
            {
                // Preserve the trailing trivia, attach this trivia to addingToken

                var srcTokenTrivia = srcToken.GetTrailingTrivia();
                srcToken = srcToken.ReplaceTrivia(srcTokenTrivia, (t1, t2) => default(SyntaxTrivia));
                srcToken = srcToken.WithTrailingTrivia(delimiterTrivias);

                addingToken = addingToken.WithTrailingTrivia(srcTokenTrivia);
            }
        }

        private static BaseMethodDeclarationSyntax WithModifiers(this BaseMethodDeclarationSyntax baseMethod, SyntaxTokenList modifiers)
        {
            if (baseMethod == null)
            {
                return null;
            }

            var method = baseMethod as MethodDeclarationSyntax;
            if (method != null)
            {
                return method.WithModifiers(modifiers);
            }

            var op = baseMethod as OperatorDeclarationSyntax;
            if (op != null)
            {
                return op.WithModifiers(modifiers);
            }

            throw new NotSupportedException($"DeclarationSyntax is not supported: {baseMethod.GetType().FullName}");
        }

        private static BaseMethodDeclarationSyntax WithSemicolonToken(this BaseMethodDeclarationSyntax baseMethod, SyntaxToken semicolonToken)
        {
            if (baseMethod == null)
            {
                return null;
            }

            var method = baseMethod as MethodDeclarationSyntax;
            if (method != null)
            {
                return method.WithSemicolonToken(semicolonToken);
            }

            var op = baseMethod as OperatorDeclarationSyntax;
            if (op != null)
            {
                return op.WithSemicolonToken(semicolonToken);
            }

            throw new NotSupportedException($"DeclarationSyntax is not supported: {baseMethod.GetType().FullName}");
        }

        private static TypeSyntax GetReturnType(this BaseMethodDeclarationSyntax baseMethod)
        {
            if (baseMethod == null)
            {
                return null;
            }

            var method = baseMethod as MethodDeclarationSyntax;
            if (method != null)
            {
                return method.ReturnType;
            }

            var op = baseMethod as OperatorDeclarationSyntax;
            if (op != null)
            {
                return op.ReturnType;
            }

            throw new NotSupportedException($"DeclarationSyntax is not supported: {baseMethod.GetType().FullName}");
        }
    }
}