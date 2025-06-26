using System.Collections.Concurrent;
using FlowCreator.Models;

namespace FlowCreator.Services
{
    public class AIDocumentService
    {
        private readonly ConcurrentDictionary<Guid, AIDocument> _aiDocuments = new();

        public IEnumerable<AIDocument> GetAllAIDocuments()
        {
            return _aiDocuments.Values.Select(Clone);
        }

        public AIDocument? GetAIDocument(Guid id)
        {
            return _aiDocuments.TryGetValue(id, out var document) ? Clone(document) : null;
        }

        public AIDocument AddAIDocument(AIDocument document)
        {
            var id = Guid.NewGuid();
            document.Id = id;
            document.Version = 0;
            _aiDocuments[id] = Clone(document);
            return Clone(document);
        }

        public bool TryUpdateAIDocument(Guid id, Func<AIDocument, AIDocument> updateFunc, int maxRetries = 3)
        {
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                if (!_aiDocuments.TryGetValue(id, out var current))
                    return false;

                var updated = updateFunc(Clone(current));
                updated.Id = id;
                updated.Version = current.Version + 1;

                if (_aiDocuments.TryUpdate(id, updated, current))
                    return true;
            }

            return false;
        }

        public bool DeleteAIDocument(Guid id)
        {
            return _aiDocuments.TryRemove(id, out _);
        }

        public void ClearAll()
        {
            _aiDocuments.Clear();
        }

        private static AIDocument Clone(AIDocument source)
        {
            return new AIDocument
            {
                Id = source.Id,
                Version = source.Version,
                ConnectionReferenceLogicalName = source.ConnectionReferenceLogicalName,
                ApiName = source.ApiName,
                ApiId = source.ApiId,
                InputSchema = source.InputSchema.Clone(),
                ActionSchema = source.ActionSchema.Clone()
            };
        }
    }
}
