namespace SpinScript.Parser.Ast;

public abstract record AstNode;

public record ProgramNode(List<AstNode> Statements) : AstNode;

public record AssignmentNode(string Name, string Value, int Line, int Column) : AstNode;

public record PatternNode(string Name, Dictionary<string, string> Parameters, List<string> Steps, int Line, int Column) : AstNode;
