#if !_Full
using System.Threading.Tasks;

namespace ASTquery
{
    public class Startup
    {
        public async Task<object> Parse(object input)
        {
            var quoter = new Quoter();
            var tree = quoter.Parse((string)input);
            return tree.ToJson();
        }

        public async Task<object> Generate(object input)
        {
            var json = (string)input;
            if (json == null)
                return null;
            var tree = ApiCall.FromJsonToSyntax(json);
            return tree.ToFullString();
        }
    }
}
#endif