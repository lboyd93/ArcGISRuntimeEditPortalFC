using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Portal;
using Esri.ArcGISRuntime.Security;
using System;
using System.Threading;
using System.Windows;

namespace UCRuntimeEditPortalFCDemo
{
    public partial class MainWindow : Window
    {
        // Default portal item Id to load features from
        private const string FeatureCollectionItemId = "01a5d5b3ae6b4bf5a00b5d482a3b322b";

        // Create global objects
        ArcGISPortal _portal;
        FeatureCollection _featCollection;
        FeatureCollectionTable _featCollectionTable;
        FeatureCollectionLayer _featCollectionLayer;

        public MainWindow()
        {
            InitializeComponent();
            Initialize();
        }
        private void Initialize()
        {
            // Add a default value for the portal item Id
            CollectionItemIdTextBox.Text = FeatureCollectionItemId;

            // Create a new map with the topographic basemap and add it to the map view
            Map myMap = new Map(Basemap.CreateTopographic());
            MyMapView.Map = myMap;

            // Create and set initial map area
            Envelope initialLocation = new Envelope(-117.54, 34.04, -117.24, 33.86, SpatialReferences.Wgs84);
            myMap.InitialViewpoint = new Viewpoint(initialLocation);

            // Authorize connection to AGOL
            ConnectToAGOL();
        }
        private void OpenPortalFeatureCollectionClick(object sender, RoutedEventArgs e)
        {
            // Get the portal item Id from the user
            var collectionItemId = CollectionItemIdTextBox.Text.Trim();

            // Make sure an Id was entered
            if (string.IsNullOrEmpty(collectionItemId))
            {
                MessageBox.Show("Please enter a portal item ID", "Feature Collection ID");
                return;
            }

            // Call a function to add the feature collection from the specified portal item
            OpenFeaturesFromArcGISOnline(collectionItemId);
        }
        private async void OpenFeaturesFromArcGISOnline(string collectionItemId)
        {
            try
            {
                // Create a portal item
                PortalItem collectionItem = await PortalItem.CreateAsync(_portal, collectionItemId);

                // Verify that the item is a feature collection and add to the map
                if (collectionItem.Type == PortalItemType.FeatureCollection)
                {
                    // Create a new FeatureCollection from the item
                    _featCollection = new FeatureCollection(collectionItem);

                    // Create a layer to display the collection and add it to the map as an operational layer
                    _featCollectionLayer = new FeatureCollectionLayer(_featCollection);

                    // Add the feature collection layer to the map
                    MyMapView.Map.OperationalLayers.Add(_featCollectionLayer);
                }
                else
                {
                    MessageBox.Show("Portal item with ID '" + collectionItemId + "' is not a feature collection.", "Feature Collection");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to open item with ID '" + collectionItemId + "': " + ex.Message, "Error");
            }
        }
        private void MyMapView_GeoViewTapped(object sender, Esri.ArcGISRuntime.UI.Controls.GeoViewInputEventArgs e)
        {
            // Enable the Save button
            SaveButton.IsEnabled = true;
            // Grab a point on the map
            var mapClickPoint = e.Location;
            // Call a method to add the feature to an existing feature collection
            AddFeature(mapClickPoint);
        }
        private async void AddFeature(MapPoint mapClickPoint)
        {
            // Grab the feature collection from the operational layers and create a feature collection layer
            _featCollectionLayer = (FeatureCollectionLayer)MyMapView.Map.OperationalLayers[0];

            // Get the points table from the feature collection layer and add to the feature collection table for editing
            _featCollectionTable = (FeatureCollectionTable)_featCollectionLayer.Layers[1].FeatureTable;

            // Create a feature
            Feature f = _featCollectionTable.CreateFeature();

            // Populate the feature's geometry with the map point coords
            f.Geometry = mapClickPoint;

            // Add the feature to the feature collection table
            await _featCollectionTable.AddFeatureAsync(f);
        }
        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Load the feature collection table
            await _featCollectionTable.LoadAsync();

            // Save the feature collection
            await _featCollection.SaveAsync();

            // Disable the Save button until next map tap
            SaveButton.IsEnabled = false;
        }
        private async void ConnectToAGOL()
        {
            // Generate an ArcGISTokenCredential using input from the user (username and password)
            var cred = await AuthenticationManager.Current.GenerateCredentialAsync(
                    new Uri("http://ess.maps.arcgis.com/sharing/rest/content/items/183eecef7a9e49f2b1b78253d035f2da/data"),
                    "MelanieWhite", "au282009") as ArcGISTokenCredential;

            // Connect to the portal, pass in the token 
            _portal = await ArcGISPortal.CreateAsync(
                    new Uri("http://ess.maps.arcgis.com/sharing/rest/content/items/183eecef7a9e49f2b1b78253d035f2da/data"),
                    cred, CancellationToken.None);

        }


    }
}