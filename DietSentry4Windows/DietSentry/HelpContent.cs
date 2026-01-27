namespace DietSentry
{
    public static class HelpContent
    {
        public static string FoodsTableTitle => FormatHelpTitle("Foods Table");

        public const string FoodsTableBody = """
This is the main screen of the app.

Its purpose is to display a list of foods from the Foods table and allow interaction with a selected food. The primary purpose being to **LOG the selected food**. The top row also contains buttons for various actions you can take.
***
### **Explanation of GUI elements**
The GUI elements on the screen are (starting at the top left hand corner and working across and down):   
- **help button** `?` which displays this help screen.
- **heading** of this screen [Foods Table]. 
- **Row of buttons** used to accomplish various tasks. If a button press opens a new screen press its help button to detail its functionality:
    - **Eaten Table** button which transfers you to the [Eaten Table] screen. 
    - **Weight Table** button which transfers you to the [Weight Table] screen.
    - **Add Solid** button which transfers you to the [Add Solid Food] screen.
    - **Add Liquid** button which transfers you to the [Add Liquid Food] screen.
    - **Add Recipe** button which transfers you to the [Add Recipe] screen.
    - **JSON** button which transfers you to the [Add Food using JSON] screen.
    - **Export db** which exports/copies the apps internal foods.db database file to the location shown in the dialog window that appears.
    - **Import db** which imports/copies a foods.db database file from the location shown in the dialog window that appears and into the **internal location** used by this app.
    - **Export csv** which exports a csv version of an aggregated form of the Eaten table to a location shown in the dialog window that appears. In particular it is the text form of what is displayed in the [Eaten Table] screen with the All option selected. the [Daily totals] option checked and no [Filter by date]. In addition the Weight table comments are included between the My Weight and Amount columns.        
- **Radio buttons** with three options (Min, NIP, All). The selection is persistent within and between app restarts. 
    - **Min**: only displays the text description of food items.
    - **NIP**: additionally displays the minimum mandated nutrient information (per 100g or 100mL of the food) as required by FSANZ on Nutritional Information Panels (NIPs).
    - **All**: displays all nutrient fields stored in the Foods table (there are 23, including Energy) PLUS the notes text field.
- **Text field** which when empty faintly displays the text "Enter food filter text"
    - Type any text in the field and press the Enter key or equivalent. This filters the list of foods to those that contain this text anywhere in their description.
    - You can also type \{text1\}|\{text2\} to match descriptions that contain BOTH of these terms.
    - It is persistent within app restarts unless explicitly modified or cleared by some secondary actions.
    - It is NOT case sensitive and is empty on app restart.
- **Clear** button which clears the above text field.    
- **Scrollable table viewer** which displays records from the Foods table. When a particular food is selected (by tapping it and then maybe scrolling) a selection panel appears at the bottom of the screen. It displays the description of the selected food followed by five buttons below it. These relate to possible actions that reference the selected food record:
    - **LOG**: logs the selected food into the Eaten Table.
        - It opens a dialog box where you can specify the amount eaten as well as the date and time (in 24hr format) that this has occurred (with the default being now).
        - Press the **Confirm** button when you are ready to log your food. This transfers focus to the [Eaten Table] screen where the just logged food will be visible. Read the help on that screen for more info.
        - If the Amount is blank or not a valid positive number pressing the **Confirm** button will display an **Invalid amount dialog**.
        - You can abort this process by tapping anywhere outside the dialog box or pressing the **Cancel** button. This closes the dialog.
    - **Edit**: allows editing of the selected food.
        - It opens the [Editing Solid Food] or [Editing Liquid Food] screens unless the description contains "\{recipe=...g\}", in which case it opens the [Editing Recipe] screen.
        - Read the help on those screens for more info.
    - **Copy**: makes a copy of the selected food.
        - If the selected food is a Solid it opens a screen titled [Copying Solid Food].
        - If the selected food is a Liquid it opens a screen titled [Copying Liquid Food].
        - If the selected food is a Recipe it opens a screen titled [Copying Recipe].
        The type of a food (Solid, Liquid or Recipe) is coded in its description field, as explained in the next section.
    - **Convert**: converts a liquid food to a solid. 
        - If the food is a liquid it displays a dialog that enables the foods density to be input in g/mL.
        - A new solid food is then created on a per 100g basis using that density.
        - The description of this new food removes the trailing " mL" marker and appends "\{density=...g/mL\}" plus the user-added "#" suffix, so you can see how it was derived.
        - If the selected food is a solid (or recipe) a [Not available] dialog is shown instead.
    - **Delete**: deletes the selected food from the database.
        - It opens a dialog which warns you that you will be deleting the selected food.
        - This is irrevocable if you press the **Confirm** button.
        - You can abort this process by tapping anywhere outside the dialog box or pressing the **Cancel** button. This closes the dialog.
---
### **Foods table structure**
```
Field name          Type    Units

FoodId              INTEGER	
FoodDescription     TEXT	
Energy              REAL    kJ
Protein             REAL    g
FatTotal            REAL    g
SaturatedFat        REAL    g
TransFat            REAL    mg
PolyunsaturatedFat  REAL    g
MonounsaturatedFat  REAL    g
Carbohydrate        REAL    g
Sugars              REAL    g
DietaryFibre        REAL    g
SodiumNa            REAL    mg
CalciumCa           REAL    mg
PotassiumK          REAL    mg
ThiaminB1           REAL    mg
RiboflavinB2        REAL    mg
NiacinB3            REAL    mg
Folate              REAL    µg
IronFe              REAL    mg
MagnesiumMg         REAL    mg
VitaminC            REAL    mg
Caffeine            REAL    mg
Cholesterol         REAL    mg
Alcohol             REAL    g
notes               TEXT
```
The FoodId field is never explicitly displayed or considered. It is a Primary Key that is auto incremented when a record is created.
The values of nutrients are per 100g or 100mL as appropriate and the units are as mandated in the FSANZ code.
The notes field is optional free text and is shown only when the All option is selected.

- **If a FoodDescription ends in the characters ` mL` or ` mL#`** the food is considered a Liquid, and nutrient values are per 100mL, The `#` character indicates that it is not part of the original database of foods.

- **If a FoodDescription ends in the characters ` {recipe=[weight]g}`** the food is considered a Recipe and can only be made up of solids and thus its nutrient values are per 100g. It is also never a part of the original database.

- **If a FoodDescription ends in any other pattern of characters than those specified above** the food is considered a Solid, and nutrient values are per 100g. If additionally it ends in ` #` then it is also never a part of the original database.
- **Foods converted from liquids** include a `{density=...g/mL}` marker in the description to record the density used for conversion.

### **Mandatory Nutrients on a NIP**
Under Standard 1.2.8 of the FSANZ Food Standards Code, most packaged foods must display a NIP showing:
- Energy (in kilojoules, and optionally kilocalories)
- Protein
- Fat (total)
- Saturated fat (listed separately from total fat)
- Carbohydrate (total)
- Sugars (listed separately from total carbohydrate)
- Sodium (a component of salt)

These values must be shown per serving and per 100 g (or 100 mL for liquids).

- **The Foods table includes these mandatory nutrients.**
 
### **When More Nutrients Are Required**
Additional nutrients must be declared if a nutrition claim is made. For example:
- If a product claims to be a “good source of fibre,” then dietary fibre must be listed.
- If a claim is made about specific fats (e.g., omega-3, cholesterol, trans fats), those must also be included.

- **The Foods table includes most such possible additional nutrients.**

### **Formatting Rules**
- Significant figures: Values must be reported to no more than three significant figures.
- Decimal places: Protein, fat, saturated fat, carbohydrate, and sugars are rounded to 1 decimal place if under 100 g. Energy and sodium are reported as whole numbers (no decimals).
- Serving size: Determined by the food business, but must be clearly stated.

- **The Foods table does not explicitly consider servings, though they might be noted in the FoodDescription text field or notes.** 

### **Exemptions**
Some foods don’t require a NIP unless a nutrition claim is made:
- Unpackaged foods (e.g., fresh fruit, vegetables)
- Foods made and packaged at point of sale (e.g., bakery bread)
- Herbs, spices, tea, coffee, and packaged water (no significant nutritional value)

- **Notwithstanding the above the Foods table includes many such items**
***
""";

        public static string EatenTableTitle => FormatHelpTitle("Eaten Table");

        public const string EatenTableBody = """
The main purpose of this screen is to **display a log of foods** you have consumed. You can also change their time stamps, amount eaten or delete them.
***
## Explanation of GUI elements
The GUI elements on the screen are (starting at the top left hand corner and working across and down):   
- **help button** `?` which displays this help screen.
- **heading** of this screen [Eaten Table]. 
- **Foods Table button** which transfers you to the [Foods Table] screen. It is slightly "dimmer" to indicate that it is a navigation button. This can also be accomplished by the `<-` navigation button at the very top left of this screen.
- **Radio buttons** with three options (Min, NIP, All). The selection is persistent between app restarts. 
    - **Min**: There are two cases:
        - when the [Daily totals] checkbox is **unchecked**, logs for individual foods are displayed comprising three rows:
            - The time stamp of the log (date+time), eg "12-Apr-24 14:30".
            - The food description in bold.
            - The amount consumed (in g or mL as appropriate) followed by the Energy (in kJ), all in 0dp, eg. "150g, 630kJ". 
        - when the [Daily totals] checkbox is **checked**, logs consolidated by date are displayed comprising six rows:
            - The date of the foods time stamp
            - The text "Daily totals" in bold
            - The total amount consumed on the day, labeled as g, mL, or "g or mL" if both are present. Amounts are still summed numerically, so mixed units are only an approximation if densities differ significantly from 1.
            - The total Energy (kJ), Fat, total (g), and Dietary Fibre (g) for the day.
    - **NIP**: There are two cases:
        - when the [Daily totals] checkbox is **unchecked**, logs for individual foods are displayed comprising ten rows:
            - The time stamp of the log (date+time).
            - The food description in bold.
            - The amount consumed (in g or mL as appropriate).
            - The seven quantities mandated by FSANZ as the minimum required in a NIP. 
        - when the [Daily totals] checkbox is **checked**, logs consolidated by date are displayed comprising ten rows:
            - The date of the foods time stamp.
            - The text "Daily totals" in bold.
            - The total amount consumed on the day, labeled as g, mL, or "g or mL".
            - The seven quantities mandated by FSANZ as the minimum required in a NIP, summed across all of the days food item logs.
    - **All**: There are two cases:
        - when the [Daily totals] checkbox is **unchecked**, logs for individual foods are displayed comprising 26 rows:
            - The time stamp of the log (date+time).
            - The food description in bold.
            - The amount consumed (in g or mL as appropriate).
            - The 23 nutrient quantities we can record in the Foods table (including Energy). 
        - when the [Daily totals] checkbox is **checked**, logs consolidated by date are displayed comprising 27 rows (or 28 if comments exist):
            - The date of the foods time stamp.
            - The text "Daily totals" in bold.
            - The text "Comments:" followed by any Weight table comments for that date (or NA if not recorded or blank).
            - The text "My weight (kg)" followed by the corresponding weight entry for that date (or NA if not recorded).
            - The total amount consumed on the day, labeled as g, mL, or "g or mL".
            - The 23 nutrient quantities we can record in the Foods table (including Energy), summed across all of the days food item logs.
- **check box** labeled "Daily totals"
    - When **unchecked** logs of individual foods eaten are displayed
    - When **checked** these logs are summed by day, giving you a daily total of each nutrient consumed (as well as Energy), even though which ones are displayed is determined by which radio button (Min, NIP, All) is pressed. 
- **check box** labeled "Filter by Date"
    - When **unchecked** all food logs are displayed. For all dates and times.
    - When **checked** only food logs of foods logged during the displayed date are displayed, whether summed or not.
- **date dialog** which displays a selected date.
    - When this app is started the default is today's date. It remains persistent while the app stays open.
- **scrollable table viewer** which displays records (possibly consolidated by date) from the Eaten table. If a particular logged food is selected (by tapping it and then maybe scrolling) a selection panel appears at the bottom of the screen. It displays the description of the selected food log and its time stamp followed by two buttons below it:
    - **Edit**: It enables the amount and time stamp of the logged eaten food to be modified.
        - It opens a dialog box where you can specify the amount eaten as well as the date and time this has occurred (with the default being now).
        - Press the **Confirm** button when you are ready to confirm your changes. This then transfers focus back to the [Eaten Table] screen where the just modified food log will be visible and selected. The selection panel for this log (with the Edit and Delete buttons) will close.
        - You can abort this process by pressing the **Cancel** button or tapping anywhere outside the dialog box. This closes it and transfers focus in the same way as described above.
    - **Delete**: deletes the selected food log from the Eaten table.
        - It opens a dialog which warns you that you will be deleting the selected food log from the Eaten table.
        - This is irrevocable if you press the **Confirm** button.
        - You can change you mind about doing this by pressing the **Cancel** button or tapping anywhere outside the dialog box. This closes it and transfers focus in the same way as described above.
    - If food logs consolidated by date are displayed (ie. the "Daily totals" check box is ticked), selection for editing or deletion is NOT POSSIBLE, so nothing happens.
***
# **Eaten table structure**
```
Field name              Type    Units

EatenId                 INTEGER
DateEaten               TEXT    d-MMM-yy
TimeEaten               TEXT    HH:mm
EatenTs                 INTEGER
AmountEaten             REAL    g or mL
FoodDescription         TEXT	
Energy                  REAL    kJ
Protein                 REAL    g
FatTotal                REAL    g
SaturatedFat            REAL    g
TransFat                REAL    mg
PolyunsaturatedFat      REAL    g
MonounsaturatedFat      REAL    g
Carbohydrate            REAL    g
Sugars                  REAL    g
DietaryFibre            REAL    g
SodiumNa                REAL    mg
CalciumCa               REAL    mg
PotassiumK              REAL    mg
ThiaminB1               REAL    mg
RiboflavinB2            REAL    mg
NiacinB3                REAL    mg
Folate                  REAL    µg
IronFe                  REAL    mg
MagnesiumMg             REAL    mg
VitaminC                REAL    mg
Caffeine                REAL    mg
Cholesterol             REAL    mg
Alcohol                 REAL    g
```
The **EatenId** field is never explicitly displayed or considered. It is a Primary Key that is auto incremented when a record is created.

The **DateEaten** and **TimeEaten** text fields store the food logs time stamp

The **EatenTs** field is an integer that specifies the number of minutes since a reference time stamp. It allows easy sorting by date/time of when a food was logged (it is re1calculated if the Date and Time eaten are changed. 

The **FoodDescription** is the same field as for a Foods table record.

The remaining (**Energy** and **Nutrient fields**) are the same as for the corresponding Foods table record, except that they are scaled by the amount of the food eaten. Eg. if EatenAmount=300 then all these field values are multiplied by 3.
***
""";

        public const string EditLiquidFoodBody = """
- These are foods for which the Energy and nutrient values are given on a **per 100mL basis**
- On first displaying this screen all the input fields will be populated with values from the selected Food, however the Description field will have the " mL" or " mL#" markers omitted. These will be reinstated after edit confirmation. This means you cannot change a Liquid food into a Solid one directly through editing. Use the **Convert** button on the [Foods Table] screen to create a new solid (annotated with a density marker) and edit that instead.
- Modify fields as desired, using decimals where needed and press the **Confirm** button to save your changes. The description field cannot be blank and the remaining fields must be non-negative numbers or blank (which is interpreted as 0). In error cases pressing the Confirm button displays an appropriate dialog ([Missing description] or [Invalid value] prompting for correction. Focus will then return back to the [Editing Liquid Food] screen after pressing the error dialog's OK button, or tapping anywhere outside this dialog.
- Notes is an optional free text (multi-line) field. It is saved with the food and shown in the [Foods Table] screen when All is selected.
- You can press either the "dimmed" **Foods Table button** or the `<-` button (in the top left hand corner of the screen) to cancel the editing process and return focus to the [Foods Table] screen.
- If confirmation succeeds the selected food is amended and focus passes to the [Foods Table] screen with the filter text being set to the just edited foods description (with markers reinstated). This allows you to review the results of the edit and is especially important if the description has changed significantly and you would not have been able to find the food.
""";

        public const string EditSolidFoodBody = """
- These are foods for which the Energy and nutrient values are given on a **per 100g basis**
- On first displaying this screen all the input fields will be populated with values from the selected Food, however the Description field will have the " #" marker omitted. These will be reinstated after edit confirmation.
- Modify fields as desired, using decimals where needed and press the **Confirm** button to save your changes. The description field cannot be blank and the remaining fields must be non-negative numbers or blank (which is interpreted as 0). In error cases pressing the Confirm button displays an appropriate dialog ([Missing description] or [Invalid value] prompting for correction. Focus will then return back to the [Editing Solid Food] screen after pressing the error dialog's OK button, or tapping anywhere outside this dialog.
- Notes is an optional free text (multi-line) field. It is saved with the food and shown in the [Foods Table] screen when All is selected.
- You can press either the "dimmed" **Foods Table button** or the `<-` button (in the top left hand corner of the screen) to cancel the editing process and return focus to the [Foods Table] screen.
- If confirmation succeeds the selected food is amended and focus passes to the [Foods Table] screen with the filter text being set to the just edited foods description (with any markers reinstated). This allows you to review the results of the edit and is especially important if the description has changed significantly and you would not have been able to find the food. 
""";

        public const string CopyLiquidFoodBody = """
- When the **Copy** button is pressed from the [Foods Table] screen with a **liquid food selected** this screen headed [Copying Liquid Food] is displayed.
- Its layout and presentation is identical to the [Editing Liquid Food] screen, the difference being that instead of modifying the selected food record, a new one is created with the displayed field values.
- Read the help from the [Editing Liquid Food] screen for analagous details of field entry and validation.
""";

        public const string CopySolidFoodBody = """
- When the **Copy** button is pressed from the [Foods Table] screen with a **Solid food selected** this screen headed [Copying Solid Food] is displayed.
- Its layout and presentation is identical to the [Editing Solid Food] screen, the difference being that instead of modifying the selected food record, a new one is created with the displayed field values.
- Read the help from the [Editing Solid Food] screen for analagous details of field entry and validation.
""";

        public const string AddSolidFoodBody = """
- When the **Add Solid** button is pressed from the [Foods Table] screen this screen is displayed.
- Its layout and presentation is identical to the [Copying Solid Food] screen, the difference being that ALL the input fields are initially empty.
- the final FoodDescription field (of the created food record) will terminate with the marker " #". You do not need to do this explicitly in the Description field.
- Read the help from the [Editing Solid Food] screen for analagous details of field entry and validation.
""";

        public const string AddLiquidFoodBody = """
- When the **Add Liquid** button is pressed from the [Foods Table] screen this screen is displayed.
- Its layout and presentation is identical to the [Copying Liquid Food] screen, the difference being that ALL the input fields are initially empty.
- the final FoodDescription field (of the created food record) will terminate with the marker " mL#". You do not need to do this explicitly in the Description field.
- Read the help from the [Editing Liquid Food] screen for analagous details of field entry and validation.
""";

        public const string JsonFoodBody = """
- When the **JSON** button is pressed from the [Foods Table] screen, this screen called [Add Food using JSON] is displayed.
- Its purpose is to allow a food (liquid or solid, but **not recipe**) to be added to the Foods table by pasting or entering JSON text that specifies the food.

### **Explanation of GUI elements**
The GUI elements on the screen are (starting at the top left hand corner and working across and down):   
- **help button** `?` which displays this help screen.
- **heading** of this screen [Foods Table]. 
- **Foods Table button** which transfers you to the [Foods Table] screen. It is slightly "dimmer" to indicate that it is a navigation button. This can also be accomplished by the `<-` navigation button at the very top left of this screen.
- **Confirm** button. Press this to process the JSON text entered in the text field below.
- **Text field** which takes up the rest of the screen. When empty it faintly displays the text "Paste JSON here.
    - The format of the JSON text needs to be precisely as shown in the example below:
    ```
    {
      "FoodDescription": "Cheese, Mersey Valley Classic #",
      "Energy": 1690,
      "Protein": 23.7,
      "FatTotal": 34.9,
      "SaturatedFat": 22.4,
      "TransFat": 1,
      "PolyunsaturatedFat": 0.5,
      "MonounsaturatedFat": 10,
      "Carbohydrate": 0.1,
      "Sugars": 0.1,
      "DietaryFibre": 0,
      "SodiumNa": 643,
      "CalciumCa": 720,
      "PotassiumK": 100,
      "ThiaminB1": 0,
      "RiboflavinB2": 0.3,
      "NiacinB3": 0.1,
      "Folate": 10,
      "IronFe": 0.2,
      "MagnesiumMg": 30,
      "VitaminC": 0,
      "Caffeine": 0,
      "Cholesterol": 100,
      "Alcohol": 0,
      "notes": "Used on-pack NIP for core nutrients. Remaining micronutrients estimated
                from AFCD/NUTTAB cheddar cheese equivalents. Website not checked—no URL 
                provided."
    }
    ```
    NOTE: **Any line feeds, tabs and spaces outside of "any text" are entirely optional** which means that this Json text is also valid though not easy to read for a human:

     ```
    {"FoodDescription":"Cheese, Mersey Valley Classic #",
    "Energy":1690,"Protein":23.7,"FatTotal":34.9,"SaturatedFat":22.4,"TransFat":1,
    "PolyunsaturatedFat":0.5,"MonounsaturatedFat":10,"Carbohydrate":0.1,"Sugars":0.1,
    "DietaryFibre":0,"SodiumNa":643,"CalciumCa":720,"PotassiumK":100,"ThiaminB1":0,
    "RiboflavinB2":0.3,"NiacinB3":0.1,"Folate":10,"IronFe":0.2,"MagnesiumMg":30,
    "VitaminC":0,"Caffeine":0,"Cholesterol":100,"Alcohol":0,"notes":"Used on-pack 
    NIP for core nutrients. Remaining micronutrients estimated from AFCD/NUTTAB 
    cheddar cheese equivalents. Website not checked—no URL provided."}   
    ```
    - press the **Confirm** button to process the JSON. This adds the food to the Foods table. Focus will then pass to the [Foods Table] screen with the filter text being set to the just created food's Description (with the liquid marker appended if relevant). This allows you to review the results of the food's creation, with this being especially important if the Description is unintuitive and finding the food in the table might be difficult.
        - If the Json text is missing or invalid an error dialog titled "Invalid JSON" will appear.
    - **To abort any actions on this screen** you can press either the "dimmed" **Foods Table button** or the `<-` button (in the top left hand corner of the screen).
***
### **AI generation of JSON**
The easiest and supported way of obtaining JSON text is to use AI. The following workflow is recommended:
- You are assumed to have access to the ChatGPT Pro paid plan (or better). This give you access to GPTs.
- Log into ChatGpt (https://chatgpt.com) and Explore GPTs. Find the **Nutrition Information Panel (NIP) generator** GPT with the following description:

    ```
    Given a food description and/or images, 
    returns its Nutrition Information Panel (NIP) in a specific JSON format. 
    It follows the FSANZ standard 1.2.8 and Schedules 11–12. 
    It can be directly added to the Foods table of the Diet Sentry apps database. 
    https://github.com/namor5772/DietSentry4Windows.
    ```
- Start chatting
- You can attach photos of labels and product NIPs to the chat prompt as well as just a text description of the food you are interested in.
- A Diet Sentry compatible JSON text will (almost always) be generated as a chat response. Copy and paste this into the text field on this screen.
- You can edit this text as desired, eg. to tweak the FoodDescription field, but make sure it remains valid JSON text.
""";

        public const string WeightTableBody = """
- **Weight Table**: a scrollable table viewer which displays records from the weight table.
    - Records are displayed in descending date order.
    - When any record is selected (by tapping it) a selection panel appears at the bottom of the screen. It displays details of the selected record followed by three buttons below it:
        - **Add**: It enables a weight record to be added to the Weight table.
            - It opens the **Add weight** dialog so you can enter a new weight and date.
            - You can optionally enter Comments for the weight entry.
            - The original selected weight record has no relevance to this activity. It is just a way of making the Add button available.
            - You cannot use a date that already exists.
            - Press the **Confirm** button when you are ready to confirm your changes. This wll be ignored if the date already exists or the weight is not a number or is blank. in these cases an appropriate Toast will be temporarily displayed.
        - **Edit**: It enables the selected weight record to be modified.
            - It opens the **Edit weight** dialog where you can modify the weight in kg. The date is shown but not editable.
            - You can edit Comments for the weight entry.
            - Press the **Confirm** button when you are ready to confirm your changes. This then transfers focus back to this screen where the just modified weight record will be visible. The selection panel is also closed.
        - **Delete**: It deletes the selected weight record.
            - It opens the **Delete weight?** warning dialog box.
            - Press the **Confirm** button when you are ready to confirm the delete. This then transfers focus back to this screen where the deleted weight record will be disappear from this scrollable table viewer. The selection panel is also closed.
        - You can abort these processes (from the above dialogs) by tapping anywhere outside the dialog box or pressing the "back" button on the bottom menu. This closes the dialog and transfers focus back to this screen. The selection panel is also closed.
    - If the Weight Table is empty (which is usually the case if the app has just been installed with the default internal databse) there is no way to enable the selection panel so that a weight record can be created by pressing the Add button. Instead the message "The Weight table has no records" is displayed, followed by the **Add** button. Press it to add a new weight record. Once at least one record exists this GUI layout disappears.
    - **The intention is** that each day at preferably the same time you weight yourself naked or with the same weight clothes and record this weight in the Weight table. When analysing your aggregated data from the Eaten table you will see your days weight together with your daily Energy and nutrient amounts. 
***
# **Weight table structure**
```
Field name          Type    Units

WeightId            INTEGER	
Weight              REAL    kg
DateWeight          TEXT    d-MMM-yy
Comments            TEXT
```
The **WeightId** field is never explicitly displayed or considered. It is a Primary Key that is auto incremented when a record is created.

The **Comments** field is optional and may be blank.

The remaining fields are self expanatory.
""";

        public static string AddFoodByJsonTitle => FormatHelpTitle("Add Food using Json");
        public static string AddLiquidFoodTitle => FormatHelpTitle("Add Liquid Food");
        public static string AddSolidFoodTitle => FormatHelpTitle("Add Solid Food");
        public static string CopyFoodTitle => FormatHelpTitle("Copy Food");
        public static string CopyRecipeTitle => FormatHelpTitle("Copying Recipe");
        public static string EditFoodTitle => FormatHelpTitle("Edit Food");
        public static string EditRecipeTitle => FormatHelpTitle("Editing Recipe");
        public static string WeightTableTitle => FormatHelpTitle("Weight Table");

        public static string AddRecipeBody => BuildRecipeHelpText("Add Recipe");
        public static string CopyRecipeBody => BuildRecipeHelpText("Copying Recipe");
        public static string EditRecipeBody => BuildRecipeHelpText("Editing Recipe");

        private const string RecipeHelpTemplate = """
# **__TITLE__**
__MODE_INTRO__

By its very nature a recipe food is more complicated than a normal liquid or solid food and hence this screen is more complex.

***
# **What is a recipe food?**
A recipe food is a record in the Foods table AND and a collection of ingredient records from the Recipe table. Each ingredient is linked to its Foods table record by the FoodId field.

For the purposes of logging consumption you select a recipe food (from the scrollable table viewer in the Foods Table screen) just like with the simpler solid and liquid foods. It is considered a solid in that its amount is measured in grams. Differences in processing are only apparent when you Edit, Add or Copy it.

You can identify a recipe food by noting that its FoodDescription field ends in text of the form " {recipe=[weight]g}" where [weight] is the total amount in grams of the ingredient foods (all required to be solids or recipes).

The **Recipe table structure** is as follows:
```
Field name              Type    Units

RecipeId                INTEGER
FoodId                  INTEGER
CopyFg                  INTEGER
Amount                  REAL    g only
FoodDescription         TEXT	
Energy                  REAL    kJ
Protein                 REAL    g
FatTotal                REAL    g
SaturatedFat            REAL    g
TransFat                REAL    mg
PolyunsaturatedFat      REAL    g
MonounsaturatedFat      REAL    g
Carbohydrate            REAL    g
Sugars                  REAL    g
DietaryFibre            REAL    g
SodiumNa                REAL    mg
CalciumCa               REAL    mg
PotassiumK              REAL    mg
ThiaminB1               REAL    mg
RiboflavinB2            REAL    mg
NiacinB3                REAL    mg
Folate                  REAL    µg
IronFe                  REAL    mg
MagnesiumMg             REAL    mg
VitaminC                REAL    mg
Caffeine                REAL    mg
Cholesterol             REAL    mg
Alcohol                 REAL    g
```

The **RecipeId** field is never explicitly displayed or considered. It is a Primary Key that is auto incremented when a record is created.

The **FoodId** field is the Primary Key of this recipe foods record in the Foods table. This is what identifies which Recipe table records are part of this particular recipe food.

The **CopyFg** field can only be 0 (default) or 1. It is used internally when a recipe food is being Edited, Added or Copied. Between any modifications to the Recipe table the value of that field is 0 for all records.
 
The **FoodDescription** is the same field as for that ingredients record in the Foods table.

The remaining (**Energy** and **Nutrient fields**) are the same as for the corresponding Foods table record (the ingredient), except that they are scaled by the amount of the food used in the recipe. Eg. if Amount=250 then all these field values are multiplied by 2.5. This is analogous to what happens to records in the Eaten table.    
   
Once all the ingredients of a recipe are known, the end markers of the recipes FoodDescription field in the Foods table record are set to " {recipe=[weight]g}" where [weight] is the total amount in grams of all the ingredient foods. Furthermore the Energy and Nutrient fields (of this Foods table record) are scaled by 100/[weight]. This guarantees that when you LOG this recipe food using [weight] as the amount consumed you will get the correct Energy and Nutrient values (as if you had consumed the meal represented by the recipe).
 
*** 
# **Explanation of GUI elements**
The GUI elements on the screen are (starting at the top left hand corner and working across and down):   
- The **heading** of the screen (for example "Add Recipe", "Editing Recipe", or "Copying Recipe"). 
- The **help button** `?` which displays this help screen.
- The **navigation button** `<-` which transfers you to the Foods Table screen.
- A **text field** titled Description.
    - __DESCRIPTION_HINT__
    - It will be the FoodDescription field of this recipe's Foods table record.
    - Once the recipe is saved by pressing the Confirm button the appropriate markers (described above) will be appended to create the final FoodDescription.
    - If left blank a toast titled "Please enter a description" will briefly appear after the Confirm button is pressed. Focus will remain on this screen.  
- A **text field** which when empty displays the text "Enter food filter text"
    - Type any text in the field and press the Enter key or equivalent. This filters the list of foods to those that contain this text anywhere in their description.
    - You can also type {text1}|{text2} to match descriptions that contain BOTH of these terms.
    - It is persistent while the app is running.
    - It is NOT case sensitive. 
- The **clear text field button** `x` which clears the above text field.    
- A **scrollable table viewer** which displays records from the Foods table.
    - When a particular food is selected (by tapping it) a dialog box appears where you can specify the amount in grams of the food that this recipe requires.
    - Press the **Confirm** button when you are ready to accept this recipe ingredient. This transfers focus back to this screen where the added ingredient will appear in the lower scrollable table viewer.
    - You can abort this process by tapping anywhere outside the dialog box. This closes it and focus returns to this screen.
    - If you select a liquid food a dialog with the title "CANNOT ADD THIS FOOD" will appear. Press the OK button or tap anywhere outside the dialog box to close it. Nothing happens and focus returns to this screen.    
- A **text label** which is of the form "Ingredients[weight] (g) Total"
    - The [weight] is the total current amount of ingredients in this recipe in grams.
    - It is automatically updated whenever the ingredients are added, edited or deleted.
- A **scrollable table viewer** which displays this recipes ingredient records from the Recipe table.
    - When an ingredient is selected (by tapping it) a selection panel appears near the bottom of the screen. It displays the description of the selected food followed by two buttons below it:
        - **Edit**: It enables the amount of the ingredient to be modified.
            - It opens a dialog box where you can modify the amount of the ingredient.
            - Press the **Confirm** button when you are ready to confirm your changes. This then transfers focus back to this screen where the just modified ingredient will be visible. The Total Ingredients amount will also be adjusted.        
            - You can abort this process by tapping anywhere outside the dialog box. This closes it and transfers focus back to this screen. The selection panel is also closed. 
        - **Delete**: deletes the selected ingredient from the Recipe table.
            - There is no warning dialog, it just irrevocably deletes the ingredient.
- Two buttons labeled **Set notes** and **Edit notes** beside the Confirm button.
    - **Set notes** fills the recipes notes field with the current ingredient list (replacing if one exists), one line per ingredient in the format "[amount] [description]".
    - **Edit notes** opens a dialog where you can directly edit (or just examine) the notes text field.
    - Notes are saved with the recipe food and shown in the Foods table when All is selected.
    - __NOTES_HINT__
- A **Confirm** button.
    - __CONFIRM_HINT__
    - If the Description is blank or there are no ingredients in the recipe an informative Toast appears and nothing changes.
    - If you want to abort any actions on this screen you can press either of the two "back" buttons which sets focus back to the Foods Table screen.    
""";

        public static string BuildRecipeHelpText(string screenTitle)
        {
            var modeIntro = screenTitle switch
            {
                "Editing Recipe" => "This screen lets you edit the recipe food you selected.",
                "Copying Recipe" => "This screen lets you create and modify a new recipe food by copying the one you selected.",
                _ => "This screen lets you create a new recipe food.",
            };

            var descriptionHint = screenTitle switch
            {
                "Add Recipe" => "It starts empty.",
                _ => "It starts with the selected recipe's description (without the recipe marker).",
            };

            var confirmHint = screenTitle switch
            {
                "Editing Recipe" => "When pressed the recipe is updated and focus shifts to the Foods Table screen.",
                "Copying Recipe" => "When pressed a new recipe is created and focus shifts to the Foods Table screen.",
                _ => "When pressed the recipe is created and focus shifts to the Foods Table screen.",
            };

            var notesHint = screenTitle switch
            {
                "Editing Recipe" => "It starts with the selected recipe's notes.",
                "Copying Recipe" => "It starts with the selected recipe's notes.",
                _ => "It starts empty.",
            };

            return RecipeHelpTemplate
                .Replace("__TITLE__", screenTitle)
                .Replace("__MODE_INTRO__", modeIntro)
                .Replace("__DESCRIPTION_HINT__", descriptionHint)
                .Replace("__CONFIRM_HINT__", confirmHint)
                .Replace("__NOTES_HINT__", notesHint);
        }

        public static string FormatHelpTitle(string title)
        {
            return $"HELP for the [{title}] screen";
        }
    }
}
