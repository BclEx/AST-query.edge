#if !Full
using System.Threading.Tasks;

namespace ASTquery
{
    public class Startup
    {
        public async Task<object> Parse(object input)
        {
            var quoter = new Quoter();
            var tree = quoter.Parse((string)input);
            return Quoter.ToJson(tree);
        }

        public async Task<object> Generate(object input)
        {
            var json = (string)input;
            if (json == null)
                return null;
            var tree = Quoter.FromJson(json);
            return tree.ToFullString();
        }
    }
}
#endif