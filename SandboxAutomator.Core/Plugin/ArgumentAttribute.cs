namespace SandboxAutomator.Core;

[AttributeUsage( AttributeTargets.Property )]
public class ArgumentAttribute : Attribute
{
	public bool Required { get; set; }
}
