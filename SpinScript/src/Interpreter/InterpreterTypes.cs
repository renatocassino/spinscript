namespace SpinScript.Interpreter;

public record SoundEvent(string Sample, double Time, int Velocity);
public record InterpretResult(List<SoundEvent> Events);
