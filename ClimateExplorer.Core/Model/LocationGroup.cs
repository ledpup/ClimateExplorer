using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClimateExplorer.Core.Model;

public class LocationGroup : LocationBase
{
    public List<Guid> LocationIds { get; set; }

    public static LocationGroup GetLocationGroup(Guid id)
    {
        return GetLocationGroups().Single(x => x.Id == id);
    }

    public static List<LocationGroup> GetLocationGroups()
    {
        return new List<LocationGroup>
        { 
            new LocationGroup
            {
                Id = new Guid("143983a0-240e-447f-8578-8daf2c0a246a"),
                Name = "112 ACORN-SAT locations, excluding 8 urban heat-influenced locations",
                LocationIds = new List<Guid>
                {
                    new Guid("cbb11150-ec74-4401-8357-fae6fef70768"),
                    new Guid("ba6fb433-bc80-4890-8c8b-16830b95f633"),
                    new Guid("05daa0ea-6227-4225-bf52-8204808709b5"),
                    new Guid("04d33b2f-2a8a-4cf0-b281-68b74475d2b4"),
                    new Guid("909dadb1-c33b-412b-97e0-42f5704f4090"),
                    new Guid("2119d5bf-94b0-4724-b2b8-bcd5f787877b"),
                    new Guid("48c35ccf-3ff8-4448-90de-1e491c500887"),
                    new Guid("1c72f3e1-7d98-4ac5-8b64-1b5b2a4d599e"),
                    new Guid("9ce454b6-7e6c-4516-88a7-6504444a4754"),
                    new Guid("503be743-9f46-4f6f-8b3e-8ce830e12fe7"),
                    new Guid("fc5ff168-2c7a-4913-bac7-957d7efa065b"),
                    new Guid("c6ba58ea-6142-4954-afac-256ca81c67bc"),
                    new Guid("02619137-0d4d-4bcf-8ae3-f87697cd178d"),
                    new Guid("f8dfe45b-d49c-4c0f-8d69-27d2d92cdb4b"),
                    new Guid("9ea5be14-0e54-4453-8e8b-4c11e075cb42"),
                    new Guid("7c7d3560-0741-4966-a878-b2749afc7737"),
                    new Guid("e491279a-781c-4eb0-8ccf-d49f3d9e2d5a"),
                    new Guid("fdd46b2e-4da4-4426-a17d-034ee9752ef6"),
                    new Guid("d7307020-9788-4269-ae25-19926ca9e198"),
                    new Guid("aa0021dd-8c27-4101-96b7-5e6b60c0c70a"),
                    new Guid("81644c2e-0ddc-472f-bae6-d976e6d623bc"),
                    new Guid("663a6037-a8de-462a-9d74-396ec881ffeb"),
                    new Guid("a7a48420-7677-444a-a6aa-e8d6eb195729"),
                    new Guid("6e8b7fce-49bf-41d2-959d-62130a40fb22"),
                    new Guid("e61fd5fb-f6a0-41b0-99bf-bc2eddb1a90c"),
                    new Guid("00147d6f-1569-422f-8322-a42b98e25071"),
                    new Guid("be54baa5-c044-41c3-affa-50884f7c9735"),
                    new Guid("5cfaf343-06a2-4bc6-a451-b8f2d3751da9"),
                    new Guid("fbb67194-9253-4ed2-bd20-f4a0211b264b"),
                    new Guid("916c204a-0847-4fd3-9ad0-0ccc5ae9c601"),
                    new Guid("b41a2fa1-ce5c-473e-b2d7-17d43e9dbc8b"),
                    new Guid("9882e6ca-a0aa-479c-b1a6-169e94481afd"),
                    new Guid("69e0e687-7e35-466a-b518-ee4c76af5cf6"),
                    new Guid("516cbbab-1226-4caa-b18d-e5b2aeeff7d6"),
                    new Guid("4397371d-a43b-4925-af58-bec79e6ca802"),
                    new Guid("6b771c0b-97d3-45c7-9cd1-72fdee232e7c"),
                    new Guid("b4e10e95-ce0f-4ae6-85cf-bc611c8164e8"),
                    new Guid("55f5699c-7d1f-4952-8bf1-4b924d7461d1"),
                    new Guid("f1be5e90-f9fd-40f4-9db8-2c5564a7e908"),
                    new Guid("b13e3137-51c7-432a-9194-2c3f42f0e438"),
                    new Guid("4865e9b4-d86a-4961-b65c-cc33f969ff04"),
                    new Guid("5885b48d-d66d-4e89-a30e-ff40a0c7d153"),
                    new Guid("bc211d96-5bf6-4427-acac-81b40e17a4ce"),
                    new Guid("1d06ff8a-82ba-4613-875f-1b4893536c42"),
                    new Guid("944b1732-d083-40b8-b865-fbf63941414c"),
                    new Guid("f0991551-814d-4323-9c0b-c8f12b887985"),
                    new Guid("fba7ed39-c752-460d-aa2e-342da7b55817"),
                    new Guid("2bf20585-6b1a-4e3c-a8ed-1d87e67789a6"),
                    new Guid("53ea40d9-d6fb-4ffe-9bde-e4188698f2de"),
                    new Guid("8fe98f5f-c239-4f2f-961a-5686147e53f7"),
                    new Guid("a5bff3b7-d9dc-40cb-bdb0-f2f07af2e2b7"),
                    new Guid("80a5afb6-f043-4e61-b1e3-b60029b9c559"),
                    new Guid("176e8056-e246-4566-b307-3c0b1ca8fde4"),
                    new Guid("9fff8b80-9230-4161-a626-c0f6e8c3b3e7"),
                    new Guid("09a5ba01-f523-4be3-ae7b-7a29a0da581c"),
                    new Guid("f285ba2b-d6d6-4390-a80f-803522668bde"),
                    new Guid("65aea1bb-5395-4b14-bf6f-8ca1559099cd"),
                    new Guid("64004954-c0c6-4de3-9ac1-3f50bada1ec9"),
                    new Guid("b2c819cd-b20b-48b0-8148-eef2f8fd88b7"),
                    new Guid("0cfb93a1-e0b6-4b87-87a6-90c0e01ca9ca"),
                    new Guid("33d2cd26-bded-4abc-8e03-2f3266e3923f"),
                    new Guid("b7e0c061-e71c-4fc4-8068-c0432398c24f"),
                    new Guid("a14c760f-4858-4f36-8d0a-7f219dc9b303"),
                    new Guid("e9581d86-1e15-42aa-ad4b-b3960a119b10"),
                    new Guid("0bc92500-5977-4aa2-81ae-a57ff8eabed3"),
                    new Guid("a205d717-c18b-4082-9822-d3309f2637f7"),
                    new Guid("bd628d1f-9c2d-403c-abb5-e2e440fd3a02"),
                    new Guid("04f7dd04-7bc2-4c4f-ae1e-2ee7180d8649"),
                    new Guid("6f523c50-bf77-44d1-8418-57ea555855a3"),
                    new Guid("bab198bd-3940-4f13-a4f7-68af10735039"),
                    new Guid("141b8a24-fc66-4ec5-a71c-2c779d802ca2"),
                    new Guid("82fbe1bf-5c58-4f9b-8f5d-71fdd060d2e9"),
                    new Guid("8867cc4a-6da5-41e6-a4f7-e02dd9460cdd"),
                    new Guid("b8a3092a-041f-478d-951f-5ee4dda0a410"),
                    new Guid("c5094f09-3fb3-4455-bf7b-b835928ad5f3"),
                    new Guid("54e1d0a7-54be-4fb9-b88e-d6a4b8a1b595"),
                    new Guid("8a63813f-8544-46b6-806e-6bc32dfc1b0d"),
                    new Guid("5d20e960-257d-4bc2-8d06-162c19fa86a2"),
                    new Guid("f0f62231-f0d3-4ded-a945-133d4ecc4255"),
                    new Guid("5a927fc5-5f9d-46a0-af66-3023610337d8"),
                    new Guid("0b927604-fbaa-4930-bb5c-cf4b0f8b8914"),
                    new Guid("5e5c8eee-e28d-4651-bb04-24d08e89c254"),
                    new Guid("6c8d1781-f74b-45b8-bfe2-8a952b278011"),
                    new Guid("1130dd09-dd90-4a8f-8a65-31f14dd449e3"),
                    new Guid("59803ca9-d81b-4888-bd08-473f659e1fc0"),
                    new Guid("c0f2743b-fb00-4128-9024-bafa4e1c85ed"),
                    new Guid("d209e7e6-d3da-40d4-b377-61cf7056d376"),
                    new Guid("f7a20e98-23e9-4920-be3c-b72f8a22754d"),
                    new Guid("2e2acf0b-592b-4ba4-adc9-b6f08546b92a"),
                    new Guid("f6093d63-0476-4bd4-bb88-6322cf04dfaa"),
                    new Guid("4006f876-24bb-4b31-9318-72fcf44f47b0"),
                    new Guid("23701518-8a46-4b7e-b9ef-da570d77b7cb"),
                    new Guid("9498f548-f000-4ff2-9c5d-fb7d3037493c"),
                    new Guid("714a7fe5-cea8-4e9b-bac0-cb01f8d0fff3"),
                    new Guid("32ecbe0c-95a6-4d39-b8f4-c7ac859c8122"),
                    new Guid("7703f086-589e-406d-b152-dde26cbe6418"),
                    new Guid("45feed6f-2821-43de-99ec-e605e3f36f75"),
                    new Guid("592c8e33-cc36-4cce-9af1-82bb3f26a5af"),
                    new Guid("1b4b65dc-5742-432d-a4ae-d83069397723"),
                    new Guid("b483cb66-25cc-45ee-b1c8-8db405f0415a"),
                    new Guid("20ab0202-8038-4275-aad2-70788ec68d6e"),
                    new Guid("eb75b5eb-8feb-4118-b6ab-bbe9b4fbc334"),
                    new Guid("db8309f6-72b8-49e2-89b3-69a13989a392"),
                    new Guid("5f6f3d62-4c80-441d-b30f-104d2f5d2e96"),
                }
            }
        };
    }
}
