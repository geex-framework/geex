import React, { useState } from "react";
import { createRoot } from 'react-dom/client';
import { ApolloProvider, useQuery, useMutation } from "@apollo/client";
import client from "./graphql/client";

import { 
  GetInitialSettings, 
  OrgCount,
  editSetting,
} from "./queries.gql";
import { SettingDefinition } from "./graphql/schema.gql";
import { loadDevMessages, loadErrorMessages } from "@apollo/client/dev";
import { SettingInfo } from "./graphql/fragments.gql";

if (process.env.NODE_ENV !== "production") {
  // Adds messages only in a dev environment
  loadDevMessages();
  loadErrorMessages();
}

const App: React.FC = () => {
  const [orgName, setOrgName] = useState<string>("example");
  const [editingSettingName, setEditingSettingName] = useState<SettingDefinition>(SettingDefinition.AppAppName);
  const [editingSettingValue, setEditingSettingValue] = useState<string>("dark");
  
  // 使用 GetInitialSettings 查询
  const { loading: settingsLoading, error: settingsError, data: settingsData } = useQuery(GetInitialSettings);
  
  // 使用 OrgCount 查询（带变量）
  const { loading: orgLoading, error: orgError, data: orgData } = useQuery(OrgCount, {
    variables: { orgName },
    skip: !orgName
  });
  
  // 使用 EditSetting 变异
  const [editSettingMutation] = useMutation(editSetting);

  const handleEditSetting = async () => {
    try {
      await editSettingMutation({
        variables: {
          name: editingSettingName,
          value: editingSettingValue
        }
      });
      console.log("Setting updated successfully");
    } catch (error) {
      console.error("Failed to update setting:", error);
    }
  };

  if (settingsLoading || orgLoading) return <p>Loading...</p>;
  if (settingsError) return <p>Settings Error: {settingsError.message}</p>;
  if (orgError) return <p>Org Error: {orgError.message}</p>;

  return (
    <div>
      <h1>GraphQL Multi-Operation Demo</h1>
      
      <section>
        <h2>Initial Settings</h2>
        <ul>
          {settingsData?.initSettings?.map((setting: SettingInfo) => (
            <li key={setting.name}>
              <strong>{setting.name}:</strong> {JSON.stringify(setting.value)}
            </li>
          ))}
        </ul>
      </section>

      <section>
        <h2>Organization Count</h2>
        <div>
          <label>
            Organization Name: 
            <input 
              value={orgName} 
              onChange={(e) => setOrgName(e.target.value)} 
              placeholder="Enter organization name"
            />
          </label>
        </div>
        
        {orgData?.orgs && (
          <div>
            <p><strong>Total Count:</strong> {orgData.orgs.totalCount}</p>
          </div>
        )}
      </section>

      <section>
        <h2>Edit Setting</h2>
        <div style={{ marginBottom: 10 }}>
          <label>
            Setting Name: 
            <input 
              value={editingSettingName} 
              onChange={(e) => setEditingSettingName(e.target.value as SettingDefinition)} 
              placeholder="e.g., THEME"
            />
          </label>
        </div>
        <div style={{ marginBottom: 10 }}>
          <label>
            Setting Value: 
            <input 
              value={editingSettingValue} 
              onChange={(e) => setEditingSettingValue(e.target.value)} 
              placeholder="e.g., dark"
            />
          </label>
        </div>
        <button onClick={handleEditSetting}>Edit Setting</button>
      </section>
    </div>
  );
};

const Root: React.FC = () => (
  <ApolloProvider client={client}>
    <App />
  </ApolloProvider>
);

const container = document.getElementById('root');
const root = createRoot(container!);
root.render(<Root />);
