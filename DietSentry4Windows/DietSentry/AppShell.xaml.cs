namespace DietSentry
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute("eatenLog", typeof(EatenLogPage));
            Routing.RegisterRoute("editFood", typeof(EditFoodPage));
            Routing.RegisterRoute("copyFood", typeof(CopyFoodPage));
            Routing.RegisterRoute("insertSolidFood", typeof(AddSolidFoodPage));
            Routing.RegisterRoute("insertLiquidFood", typeof(AddLiquidFoodPage));
            Routing.RegisterRoute("addFoodByJson", typeof(AddFoodByJsonPage));
            Routing.RegisterRoute("addRecipe", typeof(AddRecipePage));
            Routing.RegisterRoute("editRecipe", typeof(EditRecipePage));
            Routing.RegisterRoute("copyRecipe", typeof(CopyRecipePage));
            Routing.RegisterRoute("weightTable", typeof(WeightTablePage));
        }
    }
}
