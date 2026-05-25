/**
 * @name Potentially unsafe logging content in C# log calls
 * @description Flags logging calls that include sensitive placeholders or suspicious argument names.
 * @kind problem
 * @id cs/marginalia/logging-safety
 * @problem.severity error
 * @security-severity 8.2
 * @precision high
 * @tags security
 *       external/cwe/cwe-532
 */

import csharp

predicate isLoggingMethod(Method method) {
  method.getDeclaringType().hasQualifiedName("Microsoft.Extensions.Logging", "LoggerExtensions") and
  method.getName().regexpMatch("Log(Trace|Debug|Information|Warning|Error|Critical)")
}

predicate hasSensitivePlaceholder(string text) {
  text.regexpMatch(".*\\{(AccessCode|Content|FileName|Guidance|Prompt|Text|Title|Transcript)\\}.*")
}

predicate hasSensitiveArgumentName(string text) {
  text.regexpMatch(".*(providedCode|accessCode|sourceFilePath|resultFilePath|job\\.ResultFilePath|file\\.FileName|document\\.Filename|request\\.Title|effectiveUserInstructions|effectiveToneGuidance).*")
}

from Invocation invocation, Method method, Expr argument
where
  method = invocation.getTarget() and
  isLoggingMethod(method) and
  argument = invocation.getAnArgument() and
  (
    hasSensitivePlaceholder(argument.toString())
    or
    hasSensitiveArgumentName(argument.toString())
  )
select invocation,
  "Potentially unsafe log content in argument: " + argument.toString()
