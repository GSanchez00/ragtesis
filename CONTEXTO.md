# Contexto del proyecto: RAG para consultar la tesis

## Objetivo (actualizado — pivote de alcance)
Originalmente el plan era un revisor que evaluara afirmaciones de la tesis
contra un corpus de ~50 libros de teología. Se decidió **descartar la
digitalización completa de los 50 libros** (demasiado trabajo, y el
director/revisor humano de la tesis ya cumple ese rol de verificación).

**Nuevo objetivo**: un sistema RAG para **hacerle preguntas a la propia
tesis** (soteriología de Santiago el Justo, UCEL) — subir el documento,
trocearlo, vectorizarlo, y poder consultarlo en lenguaje natural. El
pipeline técnico y el código ya armado se reutilizan igual; cambia el
documento fuente y el prompt de generación (de "evaluar afirmación contra
evidencia externa" a "responder pregunta usando el propio texto").

Sigue existiendo la opción de usar una colección de corpus separada
(`corpus-libros`) si más adelante se decide agregar algún libro puntual
como referencia — pero ya no es el foco.

## Stack técnico
- **Lenguaje**: C# (.NET 9).
- **Chunking**: `MarkdownChunker.cs`, con **FluentChunker** (parsea el
  markdown por estructura real vía Markdig, da `headingPath` completo
  como metadata de sección).
- **Embeddings**: API de Gemini (`gemini-embedding-001`, free tier), vía
  `Microsoft.Extensions.AI` + `Mscc.GenerativeAI.Microsoft`. **768
  dimensiones** (confirmado empíricamente, no por la doc oficial que
  sugería 3072). Centralizado en `EmbeddingConfig.cs`.
- **Base vectorial**: **Qdrant**, local en Docker, persistente (volumen
  en `vectorstore-data/`). **Colecciones separadas por documento** — nunca
  mezclar la tesis propia con libros de referencia en la misma colección,
  para evitar que el sistema "se cite a sí mismo" como evidencia.
- **Generación**: patrón intercambiable vía `IChatClient` de
  `Microsoft.Extensions.AI` — `ChatClientFactory.cs` con
  `enum ProveedorLlm { Gemini, Nvidia }`. Modelos: `gemini-3.6-flash`
  (`gemini-2.5-flash` quedó deprecado) y Nemotron 3 Ultra 550B vía NVIDIA
  Build/NIM (SDK de OpenAI apuntado a `integrate.api.nvidia.com/v1`).
  - `RevisorService.cs`: prompt de evaluación (afirmación + evidencia →
    veredicto) — usado en la etapa anterior del proyecto, sigue
    disponible si se retoma el enfoque de corpus.
  - `ConsultaTesisService.cs` (nuevo, a implementar): prompt más simple,
    "respondé la pregunta usando este contexto" — para el nuevo objetivo.
- **Extracción PDF → Markdown**: manual, vía LLM en el chat (Claude o
  ChatGPT), capítulo por capítulo, con `append` al `.md` del documento
  completo (no pedir "el libro/tesis entera" de una sola vez — los LLMs
  tienden a resumir silenciosamente en documentos muy largos).

## Estructura de carpetas (actual, antes de reorganizar para la web)

rag-tesis/
├── data/pdfs/
├── output/
│ ├── markdown/
│ └── chunks/ # JSON cacheado del chunking
├── src/
│ ├── RagTesis.sln
│ └── RagTesis/
│ ├── Program.cs
│ ├── Models/ (ChunkMetadata, ChunkVectorRecord, EmbeddingConfig, ProveedorLlm)
│ ├── Chunking/MarkdownChunker.cs
│ ├── Embeddings/EmbeddingService.cs
│ ├── VectorStore/VectorStoreSetup.cs
│ └── Chat/ (ChatClientFactory.cs, RevisorService.cs)
├── vectorstore-data/ # datos persistentes de Qdrant
└── .gitignore


## Estructura planeada para la web (próximo paso, aún no implementado)
Reorganizar en 3 proyectos dentro de la misma solución:

src/
├── RagTesis.Core/ # toda la lógica actual, sin Program.cs — librería compartida
├── RagTesis/ # la consola actual, ahora referencia Core
└── RagTesis.Web/ # NUEVO — Blazor Server (mismo lenguaje, sin sumar stack de frontend)

**Elegido Blazor Server** en vez de API+React porque todo el proyecto ya
es C#, y evita correr dos lenguajes/proyectos distintos para algo de uso
personal (no necesita escalar a muchos usuarios concurrentes).

### Diseño de la pantalla de subida
Una página con **selector de tipo de documento**: "Mi tesis" o "Libro del
corpus" (radio buttons). Si es corpus, campo extra para el nombre del
libro. Sube un `.md` ya convertido (la conversión PDF→MD sigue siendo
manual vía chat, la web no la hace — al menos no en esta primera
versión). Según la opción elegida, el procesamiento (chunking →
embeddings → guardado) apunta a la colección `mi-tesis` o
`corpus-libros` respectivamente, usando el mismo `nombreColeccion` que ya
soporta `VectorStoreSetup`.

**Pendiente de implementar**: `MarkdownChunker` necesita un método nuevo
que reciba el texto del markdown directo (`ChunkearMarkdownDesdeTextoAsync`),
en vez de solo una ruta de archivo — porque en la web el contenido llega
como stream subido, no como archivo ya en disco.

### Diseño de la pantalla de consulta
Selector de a qué colección preguntarle, campo de texto para la
pregunta, muestra la respuesta vía `ConsultaTesisService` (para
preguntas sobre la propia tesis) o `RevisorService` (si se usa el modo
corpus/evaluación).

## Estado actual — qué está probado y funcionando
- **Pipeline completo de punta a punta**, probado con el capítulo de
  prueba de Dunn (204 chunks, 203 tras filtrar metadata sin sección):
  extracción manual → chunking (FluentChunker) → embeddings (Gemini,
  768 dim) → guardado en Qdrant (persistente) → retrieval (probado con
  una pregunta real, 5 resultados relevantes, scores 0.73-0.77) →
  generación con dos proveedores intercambiables (Gemini y NVIDIA,
  ambos corrieron exitosamente, Gemini detectó correctamente un matiz
  incorrecto puesto a propósito en una afirmación de prueba).
- **Extracción con visión (Gemini)** evaluada como alternativa de mejor
  calidad que Document Intelligence/OCR tradicional para páginas con
  layout complejo — confirmado con benchmarks reales (Gemini ~90-94% de
  precisión vs. ~81-83% de Azure Document Intelligence en documentos
  complejos). `ExtractorVisionService.cs` armado como prueba de
  concepto (no integrado al flujo principal todavía).
- Se descartó Ollama/embeddings locales por ahora (rate limit de Gemini
  no es un problema con el volumen reducido tras el pivote de alcance).

## Entorno de desarrollo
- Windows 10 + WSL2 (Ubuntu), proyecto en `~/proyectos/rag-tesis`
  (filesystem de Linux, no `/mnt/c/...`).
- Claude Code instalado en WSL (instalador nativo).
- Docker (Engine nativo, no Desktop) corriendo Qdrant con volumen
  persistente.
- VS Code + extensión WSL.
- API keys en `dotnet user-secrets`: `GoogleApiKey`, `NvidiaApiKey`.
- Paquetes de Semantic Kernel alineados en `1.74.0` / `1.74.0-preview`.
- Sin entorno virtual de Python — todo el pipeline es C#.

## Próximos pasos
1. Reorganizar el código en `RagTesis.Core` (librería compartida).
2. Crear `RagTesis.Web` (Blazor Server) referenciando `Core`.
3. Agregar `ChunkearMarkdownDesdeTextoAsync` a `MarkdownChunker`.
4. Armar `ConsultaTesisService.cs` (prompt simple de pregunta-respuesta).
5. Construir la página de subida (con el selector Tesis/Corpus) y la
   página de consulta.
6. Convertir la tesis propia a `.md` (capítulo por capítulo, con
   `append`) para tener el primer documento real que subir y probar.