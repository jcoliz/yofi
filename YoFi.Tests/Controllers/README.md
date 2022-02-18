# Controller Unit Tests

I call these "slim" tests in that they only test the controllers, which have become pretty slim,
so there's not a lot of functionality to test.

The philosophy here is that the Controller objects should be the only production objects in
the calling chain. The dependencies injected into controller construction must all be mocks.