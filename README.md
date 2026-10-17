<div align="center">

# Creole

A **layered**, **analyzable** parser-combinator library for .NET

_Currently in the early stages: there is a whitepaper and a plan, but not much else to see. Yet..._

</div>

---

I am a big fan of parser-combinators; they're fun and expressive and just computer-sciency enough to make me feel really smart for knowing about them! .NET already has a few great libraries for these (I am very bad at being a member of [Sprache](https://github.com/sprache/Sprache)), but there's been a lot of research in the space in recent years.

I'm going to take a stab at building a super-duper, modern parser-combinator library. Something with a bunch of nerdy bells and whistles that can be used for any grammar. I'm calling it Creole because that's what came to mind (one of the existing libraries is the excellent [Pidgin](https://github.com/benjamin-hodgson/Pidgin)).

In particular, I really want to be able to write a multi-stage parser (lex, token, parse, so on) with one common vocabulary, and chain them together. Issues at any stage should back-propogate, error messages should be a first-class concern throughout, the parser should be able to steer the lexer, and source generation and Roslyn analyzers should be used at every point to make the parser author's devex as smooth as possible.

So far, Creole is a [whitepaper](whitepaper.md) and a plan of work items, which I'm aware is a funny thing to publish for someone who [tells people to descope their personal projects](https://ian.wold.guru/Posts/successful_personal_projects.html). In my defense, the first phase stands on its own as a perfectly usable library, and everything after that is a bonus. C# 15 is due this November and some of the core types are waiting on it, so in the meantime I'm laying foundations.

I'm also using this as a test bed for using Claude to assist with project management and research. I'm a fan of [nockawa](https://github.com/nockawa)'s setup on his [Typhon](https://github.com/Log2n-io/Typhon) project, and I like the idea of all my cards living in markdown in my repo instead of on GitHub. So, we'll be seeing how that goes as well.

- [Whitepaper](whitepaper.md): the design.
- [Work tracker](work/README.md): roadmap, epics and cards.
- [Contributing](CONTRIBUTING.md): how work moves from idea to done.
- [Documentation](docs/README.md): the library as built.
