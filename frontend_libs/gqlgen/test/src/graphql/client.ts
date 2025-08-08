import { ApolloClient, InMemoryCache } from '@apollo/client';

const client = new ApolloClient({
  uri: 'https://api.dev.geexcode.com/graphql/',
  cache: new InMemoryCache(),
});

export default client; 
