# TODO — Conexión IA (único pendiente funcional)

El sistema está **LISTO PARA PRODUCCIÓN** excepto las integraciones con proveedores LLM/embeddings reales.

---

## Qué conectar

| Capacidad | Interfaz | Implementación actual |
|-----------|----------|------------------------|
| Agentes autónomos LLM | `IAgentService` | `PlaceholderAgentService` |
| Completions (OpenAI, Claude, Gemini) | `ILLMProvider` | `PlaceholderLlmProvider` |
| Embeddings / búsqueda semántica | `IEmbeddingService` | `PlaceholderEmbeddingService` |
| Workflows autónomos multi-paso | `IAutonomousWorkflow` | `PlaceholderAutonomousWorkflow` |

**Ubicación interfaces:** `/AI/*.cs`  
**Ubicación DI:** `AutonomusCRM.AI/DependencyInjection.cs` → `AddAiPlaceholders()`

---

## Dónde conectar (pasos)

1. Crear proyecto o carpeta `AutonomusCRM.AI.Providers.OpenAI` (u otro proveedor).
2. Implementar `ILLMProvider` llamando a la API REST del proveedor.
3. Registrar en `Program.cs` **condicionalmente**:

```csharp
if (configuration.GetValue<bool>("AI:Enabled"))
    services.AddSingleton<ILLMProvider, OpenAiLlmProvider>();
else
    services.AddAiPlaceholders(configuration);
```

4. Sustituir llamadas en agentes (`Workers/Agents/*`) para usar `IAgentService` donde hoy hay lógica heurística.
5. Añadir feature flags por tenant en `Tenant.Settings`.

---

## Configuración (`appsettings`)

```json
"AI": {
  "Enabled": false,
  "Provider": "OpenAI",
  "ApiKey": "",
  "Model": "gpt-4o-mini",
  "Endpoint": "https://api.openai.com/v1"
}
```

### Variables de entorno (producción)

| Variable | Ejemplo |
|----------|---------|
| `AI__Enabled` | `true` |
| `AI__Provider` | `OpenAI` |
| `AI__ApiKey` | `sk-...` |
| `AI__Model` | `gpt-4o-mini` |
| `AI__Endpoint` | URL base del proveedor |

**Nunca** commitear `ApiKey` en el repositorio.

---

## APIs necesarias por proveedor

| Proveedor | API | Uso |
|-----------|-----|-----|
| OpenAI | Chat Completions, Embeddings | `ILLMProvider`, `IEmbeddingService` |
| Anthropic Claude | Messages API | `ILLMProvider` |
| Google Gemini | `generateContent` | `ILLMProvider` |
| Azure OpenAI | Deployment endpoints | Enterprise / mismo contrato OpenAI |

---

## Costo aproximado (referencia 2026)

| Modelo | Entrada (1M tokens) | Salida (1M tokens) | Notas |
|--------|---------------------|---------------------|-------|
| GPT-4o mini | ~$0.15 | ~$0.60 | Recomendado para scoring/leads |
| GPT-4o | ~$2.50 | ~$10.00 | Estrategia de deals compleja |
| Claude 3.5 Sonnet | ~$3.00 | ~$15.00 | Alternativa calidad |
| Embeddings small | ~$0.02 / 1M tokens | — | Búsqueda semántica |

**Estimación mensual MVP (10k eventos/mes, ~500 tokens/evento):** USD $15–80 según modelo y cache.

---

## Seguridad

- Rotar API keys por entorno.
- Limitar rate por tenant (`RateLimiter` ya en API).
- No enviar PII sin política de redacción (`ComplianceSecurityAgent` como gate).
- Registrar prompts/responses en Event Store solo si cumple compliance.

---

## Validación post-conexión

1. `AI:Enabled=true` en staging.
2. Test unitario mock del proveedor.
3. Test integración con API key en secretos CI (opcional).
4. Monitorear coste en dashboard del proveedor.

---

*Cuando completes la integración, actualizar `FINAL_SYSTEM_STATUS.md` sección IA a 100%.*
