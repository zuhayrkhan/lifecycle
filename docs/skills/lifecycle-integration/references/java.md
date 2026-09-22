# Java integration

## Obtain dependencies

Require JDK 17+. Do not assume these artifacts are published to Maven Central.
Use the user's configured artifact repository or build the selected Lifecycle
checkout into the local Maven cache:

```sh
# In the Lifecycle checkout; this route requires only the Java toolchain.
./mvnw -pl java-libraries/lifecycle-otel -am install
```

In the consumer, add `io.github.zuhayrkhan.lifecycle:lifecycle-core:0.1.0` and,
for OTel, `io.github.zuhayrkhan.lifecycle:lifecycle-otel:0.1.0`. The adapter uses
the OTel API; the host supplies SDK and exporter dependencies. Preserve existing
dependency management and verify compatibility if it uses another OTel version.

This standalone console smoke example uses the tested baseline. Its full `pom.xml`
does not require inheriting from Lifecycle's reactor:

```xml
<project xmlns="http://maven.apache.org/POM/4.0.0"
         xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
         xsi:schemaLocation="http://maven.apache.org/POM/4.0.0 https://maven.apache.org/xsd/maven-4.0.0.xsd">
  <modelVersion>4.0.0</modelVersion>
  <groupId>example</groupId>
  <artifactId>lifecycle-consumer</artifactId>
  <version>1.0.0-SNAPSHOT</version>
  <properties>
    <maven.compiler.release>17</maven.compiler.release>
    <project.build.sourceEncoding>UTF-8</project.build.sourceEncoding>
  </properties>
  <dependencyManagement>
    <dependencies>
      <dependency>
        <groupId>io.opentelemetry</groupId>
        <artifactId>opentelemetry-bom</artifactId>
        <version>1.66.0</version>
        <type>pom</type>
        <scope>import</scope>
      </dependency>
    </dependencies>
  </dependencyManagement>
  <dependencies>
    <dependency>
      <groupId>io.github.zuhayrkhan.lifecycle</groupId>
      <artifactId>lifecycle-core</artifactId>
      <version>0.1.0</version>
    </dependency>
    <dependency>
      <groupId>io.github.zuhayrkhan.lifecycle</groupId>
      <artifactId>lifecycle-otel</artifactId>
      <version>0.1.0</version>
    </dependency>
    <dependency>
      <groupId>io.opentelemetry</groupId>
      <artifactId>opentelemetry-sdk</artifactId>
    </dependency>
    <dependency>
      <groupId>io.opentelemetry</groupId>
      <artifactId>opentelemetry-exporter-logging</artifactId>
    </dependency>
  </dependencies>
  <build>
    <plugins>
      <plugin>
        <groupId>org.apache.maven.plugins</groupId>
        <artifactId>maven-compiler-plugin</artifactId>
        <version>3.14.1</version>
      </plugin>
      <plugin>
        <groupId>org.codehaus.mojo</groupId>
        <artifactId>exec-maven-plugin</artifactId>
        <version>3.6.4</version>
      </plugin>
    </plugins>
  </build>
</project>
```

## Wire the host

Save as `src/main/java/example/LifecycleHost.java` in that smoke project:

```java
package example;

import io.github.zuhayrkhan.lifecycle.ConditionState;
import io.github.zuhayrkhan.lifecycle.DefaultLifecycle;
import io.github.zuhayrkhan.lifecycle.Lifecycle;
import io.github.zuhayrkhan.lifecycle.LifecyclePhase;
import io.github.zuhayrkhan.lifecycle.otel.OpenTelemetryLifecycleSink;
import io.opentelemetry.api.common.Attributes;
import io.opentelemetry.exporter.logging.SystemOutLogRecordExporter;
import io.opentelemetry.sdk.logs.SdkLoggerProvider;
import io.opentelemetry.sdk.logs.export.BatchLogRecordProcessor;
import io.opentelemetry.sdk.resources.Resource;
import java.util.Map;
import java.util.concurrent.TimeUnit;

public final class LifecycleHost {
    public static void main(String[] args) {

        try (SdkLoggerProvider provider = SdkLoggerProvider.builder()
                .setResource(Resource.create(Attributes.builder()
                        .put("service.name", "lifecycle-consumer").build()))
                .addLogRecordProcessor(BatchLogRecordProcessor.builder(
                        SystemOutLogRecordExporter.create()).build()).build()) {
            Lifecycle lifecycle = new DefaultLifecycle(new OpenTelemetryLifecycleSink(provider));

            lifecycle.phase(LifecyclePhase.STARTING);
            lifecycle.milestone("configuration-loaded", Map.of("config.cached", true));
            lifecycle.condition("dependency.database", ConditionState.UP);
            lifecycle.phase(LifecyclePhase.RUNNING);

            // The real application runs here, sharing this Lifecycle instance.

            lifecycle.phase(LifecyclePhase.STOPPING);
            // Finish application shutdown work before declaring STOPPED.
            lifecycle.phase(LifecyclePhase.STOPPED);

            if (!provider.forceFlush().join(10, TimeUnit.SECONDS).isSuccess()) {
                System.err.println("Lifecycle telemetry flush did not complete successfully");
            }
        }
    }
}
```

Run `mvn compile exec:java -Dexec.mainClass=example.LifecycleHost` in the consumer
(or its Maven wrapper). This exports six structured records using the SDK's console
exporter. It demonstrates successful startup and orderly shutdown only; place the
calls into the real host's existing success/failure/cleanup paths.

In an existing host, inject its `LoggerProvider` into `OpenTelemetryLifecycleSink`
and share the resulting `Lifecycle`. Do not close that provider from a component.
If the application already has an OTel Resource, preserve it rather than replacing
it with this example's minimal `service.name` Resource.

For OTLP/HTTP, add `io.opentelemetry:opentelemetry-exporter-otlp` under the same BOM
and replace the console exporter with `OtlpHttpLogRecordExporter.builder()` configured
with the host's full logs URL (for example `http://localhost:4318/v1/logs`). Import
`io.opentelemetry.exporter.otlp.http.logs.OtlpHttpLogRecordExporter`. Reuse the host's
approved endpoint/auth configuration; do not silently add a new destination.

## Core-only tests and diagnostics

Use `InMemoryLifecycleEventSink sink = new InMemoryLifecycleEventSink();` and
`Lifecycle lifecycle = new DefaultLifecycle(sink);`, then inspect `sink.events()`.
The sink is unbounded, so use it for finite tests rather than production retention.
The constructor accepting `(sink, Clock, LongSupplier, Consumer<RuntimeException>)`
allows deterministic wall/elapsed time and failure diagnostics. Composite child
diagnostics belong on `CompositeLifecycleEventSink` itself, since child exceptions
are contained there.
