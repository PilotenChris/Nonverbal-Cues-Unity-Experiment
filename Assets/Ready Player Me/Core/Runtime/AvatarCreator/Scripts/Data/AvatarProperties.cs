using Newtonsoft.Json;
using ReadyPlayerMe.Core;
using System;
using System.Collections.Generic;

namespace ReadyPlayerMe.AvatarCreator {
    [Serializable]
    public struct AvatarProperties {
        public string Id;
        public string Partner;
        [JsonConverter(typeof(GenderConverter))]
        public OutfitGender Gender;
        [JsonConverter(typeof(BodyTypeConverter))]
        public BodyType BodyType;
        [JsonConverter(typeof(CategoryDictionaryConverter))]
        public Dictionary<AssetType, object> Assets;
        public string Base64Image;
        public bool isDraft;
    }
}
