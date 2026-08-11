namespace SpinScript.Parser.Ast;

public abstract record AstNode;

public record ProgramNode(List<AstNode> Statements) : AstNode;

public record AssignmentNode(string Name, string Value) : AstNode;
